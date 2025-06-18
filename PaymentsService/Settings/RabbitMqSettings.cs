namespace PaymentsService.Settings;

public class RabbitMqSettings
{
    public required string Host { get; set; }
    public required string InQueue { get; set; }
    public required string OutQueue { get; set; }
}