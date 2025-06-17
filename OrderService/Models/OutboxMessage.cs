namespace OrderService.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = null!;
    public string Payload { get; set; } = null!;
    public bool Published { get; set; }
    public DateTime OccurredAt { get; set; }
}