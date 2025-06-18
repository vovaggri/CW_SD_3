using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using OrderService.Data;
using OrderService.Settings;

namespace OrderService.Services
{
    public class OutboxPublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IModel _channel; 
        private readonly string _queueName;

        public OutboxPublisher(
            IServiceScopeFactory scopeFactory,
            IModel channel, 
            IOptions<RabbitMqSettings> options)
        {
            _scopeFactory = scopeFactory;
            _channel = channel;
            _queueName = options.Value.QueueName;

            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: null
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using (var scope = _scopeFactory.CreateScope())
                {
                    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                    var batch = await db.Outbox
                        .Where(x => !x.Published)
                        .OrderBy(x => x.OccurredAt)
                        .Take(20)
                        .ToListAsync(stoppingToken); 

                    if (batch.Any())
                    {
                        foreach (var msg in batch)
                        {
                            var body = Encoding.UTF8.GetBytes(msg.Payload);
                            var properties = _channel.CreateBasicProperties();
                            properties.Persistent = true;

                            _channel.BasicPublish(
                                exchange: string.Empty,
                                routingKey: _queueName,
                                basicProperties: properties,
                                body: body
                            );
                            msg.Published = true;
                        }

                        await db.SaveChangesAsync(stoppingToken);
                    }
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}