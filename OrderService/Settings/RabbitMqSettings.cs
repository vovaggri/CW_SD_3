namespace OrderService.Settings;

public class RabbitMqSettings
{
    public required string Host { get; set; }
    public required string QueueName { get; set; }
    public required string PaymentQueueName { get; set; }
}