using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitChannel = RabbitMQ.Client.IChannel;
using OrderService.Data;
using OrderService.Settings;

namespace OrderService.Services
{
    /*
    /// <summary>
    /// Фоновый сервис, читающий из таблицы Outbox и публикующий в RabbitMQ
    /// </summary>
    public class OutboxPublisher : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly RabbitChannel _channel;
        private readonly string _queueName;

        public OutboxPublisher(
            IServiceScopeFactory scopeFactory,
            IModel channel,
            IOptions<RabbitMqSettings> options)
        {
            _scopeFactory = scopeFactory;
            _channel      = channel;
            _queueName    = options.Value.QueueName;

            // Объявляем очередь при старте
            _channel.QueueDeclare(
                queue: _queueName,
                durable:     true,
                exclusive:   false,
                autoDelete:  false,
                arguments:   null
            );
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                // Берём до 20 неотправленных сообщений
                var batch = await db.Outbox
                    .Where(x => !x.Published)
                    .OrderBy(x => x.OccurredAt)
                    .Take(20)

                if (batch.Count > 0)
                {
                    foreach (var msg in batch)
                    {
                        var body = Encoding.UTF8.GetBytes(msg.Payload);
                        _channel.BasicPublish(
                            exchange:    string.Empty,
                            routingKey:  _queueName,
                            basicProperties: null,
                            body:        body
                        );
                        msg.Published = true;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }

                // Задержка перед следующей итерацией
                await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
            }
        }
    }
    */
}
