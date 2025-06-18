using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using OrderService.Data;
using OrderService.Models;
using OrderService.Settings;

namespace OrderService.Services
{
    public class PaymentConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IModel               _channel;
        private readonly string               _inQueue;

        // простой класс для десериализации
        private record PaymentResult(Guid OrderId, bool Success);

        public PaymentConsumer(
            IServiceScopeFactory scopeFactory,
            IModel channel,
            IOptions<RabbitMqSettings> opts)
        {
            _scopeFactory = scopeFactory;
            _channel      = channel;
            _inQueue      = opts.Value.PaymentQueueName;

            // объявляем очередь, если её нет
            _channel.QueueDeclare(
                queue: _inQueue,
                durable:    true,
                exclusive:  false,
                autoDelete: false,
                arguments:  null
            );
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new EventingBasicConsumer(_channel);
            consumer.Received += OnMessageReceived;
            _channel.BasicConsume(queue: _inQueue, autoAck: false, consumer: consumer);
            return Task.CompletedTask;
        }

        private async void OnMessageReceived(object sender, BasicDeliverEventArgs ea)
        {
            var props       = ea.BasicProperties;
            var deliveryTag = ea.DeliveryTag;

            try
            {
                var json = Encoding.UTF8.GetString(ea.Body.ToArray());
                var result = JsonSerializer.Deserialize<PaymentResult>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (result is not null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

                    var order = await db.Orders.FindAsync(result.OrderId);
                    if (order != null)
                    {
                        order.Status = result.Success 
                            ? OrderStatus.FINISHED 
                            : OrderStatus.CANCELLED;
                        await db.SaveChangesAsync();
                    }
                }

                _channel.BasicAck(deliveryTag, false);
            }
            catch
            {
                _channel.BasicNack(deliveryTag, false, requeue: false);
            }
        }
    }
}