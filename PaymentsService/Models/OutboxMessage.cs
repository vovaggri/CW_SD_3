namespace PaymentsService.Models;

public class OutboxMessage
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public required string Payload { get; set; }
    public bool Published { get; set; }
    public DateTime OccurredAt { get; set; }
}