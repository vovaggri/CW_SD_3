public class ServiceUrls
{
    public string OrdersService  { get; set; } = "";
    public string PaymentsService{ get; set; } = "";
}

public enum OrderStatus
{
    NEW, 
    FINISHED, 
    CANCELLED
}