namespace TicketingSystem.AsyncApi.Contracts.Responses;

public class PaymentResponse
{
    public Guid PaymentId { get; set; }
    public int OrderId { get; set; }
    public string Status { get; set; } = string.Empty;
}

public class PaymentStatusUpdateResponse
{
    public Guid PaymentId { get; set; }
    public string Status { get; set; } = string.Empty;
}