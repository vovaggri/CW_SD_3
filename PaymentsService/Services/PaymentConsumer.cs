using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using PaymentsService.Data;
using PaymentsService.Models;
using PaymentsService.Settings;

namespace PaymentsService.Services
{
    /// <summary>
    /// Фоновый сервис, читающий события OrderCreated, списывающий баланс и публикующий результат
    /// </summary>
    public class PaymentConsumer : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IModel               _channel;
        private readonly string               _inQueue;
        private readonly string               _outQueue;
        private readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public PaymentConsumer(
            IServiceScopeFactory       scopeFactory,
            IModel                     channel,
            IOptions<RabbitMqSettings> opts)
        {
            _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
            _channel      = channel      ?? throw new ArgumentNullException(nameof(channel));

            var settings = opts?.Value 
                ?? throw new ArgumentNullException(nameof(opts));
            _inQueue  = settings.InQueue  
                ?? throw new InvalidOperationException("RabbitMq:InQueue not configured");
            _outQueue = settings.OutQueue 
                ?? throw new InvalidOperationException("RabbitMq:OutQueue not configured");

            // Объявляем необходимые очереди
            _channel.QueueDeclare(_inQueue,  durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(_outQueue, durable: true, exclusive: false, autoDelete: false);
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

            // 1) Получаем и проверяем MessageId
            if (!Guid.TryParse(props?.MessageId, out var messageId))
            {
                _channel.BasicAck(deliveryTag, false);
                return;
            }

            try
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

                // 2) Дедупликация
                if (await db.Inbox.AsNoTracking().AnyAsync(x => x.Id == messageId))
                {
                    _channel.BasicAck(deliveryTag, false);
                    return;
                }

                // 3) Начинаем транзакцию
                await using var tx = await db.Database.BeginTransactionAsync();

                // 4) Сохраняем в Inbox
                db.Inbox.Add(new InboxMessage {
                    Id         = messageId,
                    EventType  = props.Type        ?? string.Empty,
                    ReceivedAt = DateTime.UtcNow
                });

                // 5) Парсим тело
                var bodyText = Encoding.UTF8.GetString(ea.Body.ToArray());
                OrderCreatedDto? createdEvt = null;
                try
                {
                    createdEvt = JsonSerializer.Deserialize<OrderCreatedDto>(bodyText, _jsonOptions);
                }
                catch (JsonException)
                {
                    // здесь можно логировать некорректный формат
                }

                // 6) Списание с учётом успеха/неудачи
                var success = false;
                if (createdEvt is not null)
                {
                    var account = await db.Accounts.FindAsync(createdEvt.UserId);
                    if (account is not null && account.Balance >= createdEvt.Amount)
                    {
                        account.Balance -= createdEvt.Amount;
                        success = true;
                    }
                }

                // 7) Записываем результат в Outbox
                var result = new PaymentResult(
                    OrderId: createdEvt?.OrderId ?? Guid.Empty,
                    Success: success
                );
                db.Outbox.Add(new OutboxMessage {
                    Id         = Guid.NewGuid(),
                    EventType  = success ? "PaymentSucceeded" : "PaymentFailed",
                    Payload    = JsonSerializer.Serialize(result, _jsonOptions),
                    Published  = false,
                    OccurredAt = DateTime.UtcNow
                });

                // 8) Сохраняем и коммитим
                await db.SaveChangesAsync();
                await tx.CommitAsync();

                // 9) Подтверждаем приём
                _channel.BasicAck(deliveryTag, false);
            }
            catch (Exception ex)
            {
                // TODO: залогировать ex
                // Нельзя гарантировать успех — не возвращаем в очередь
                _channel.BasicNack(deliveryTag, false, requeue: false);
            }
        }

        // DTO для разбора входящего события
        private record OrderCreatedDto(Guid OrderId, int UserId, decimal Amount);

        // DTO для результата оплаты
        private record PaymentResult(Guid OrderId, bool Success);
    }
}
