namespace TicketingSystem.AsyncApi.Contracts.Responses;

public class CartResponse
{
    public Guid CartId { get; set; }
    public IReadOnlyCollection<CartItemResponse> Items { get; set; } = [];
    public decimal TotalAmount { get; set; }
}

public class CartItemResponse
{
    public int EventId { get; set; }
    public int SeatId { get; set; }
    public int PriceId { get; set; }
    public int RowId { get; set; }
    public decimal Amount { get; set; }
}