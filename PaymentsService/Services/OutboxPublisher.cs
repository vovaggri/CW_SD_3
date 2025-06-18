using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using PaymentsService.Data;
using PaymentsService.Models;
using PaymentsService.Settings;

namespace PaymentsService.Services
{
    public class OutboxPublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IModel _channel;
        private readonly string _outQueue;

        public OutboxPublisher(
            IServiceScopeFactory       scopeFactory,
            IModel                     channel,
            IOptions<RabbitMqSettings> opts)
        {
            _scopeFactory = scopeFactory;
            _channel      = channel;
            _outQueue     = opts.Value.OutQueue;

            _channel.QueueDeclare(_outQueue, durable: true, exclusive: false, autoDelete: false, arguments: null);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

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
                        _channel.BasicPublish(
                            exchange:       "",
                            routingKey:     _outQueue,
                            basicProperties:null,
                            body:           body);
                        msg.Published = true;
                    }
                    await db.SaveChangesAsync(stoppingToken);
                }

                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
}
