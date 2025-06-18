namespace PaymentsService.Models;

public class InboxMessage
{
    public Guid Id { get; set; }
    public required string EventType { get; set; }
    public DateTime ReceivedAt { get; set; }
}