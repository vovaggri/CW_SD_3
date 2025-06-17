namespace OrderService.Models;

public enum OrderStatus
{
    NEW, 
    FINISHED, 
    CANCELLED
}

public class Order
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}