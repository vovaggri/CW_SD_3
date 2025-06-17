namespace OrderService.Settings;

public class RabbitMqSettings
{
    public string Host { get; set; } = null!;
    public string QueueName { get; set; } = null!;
}