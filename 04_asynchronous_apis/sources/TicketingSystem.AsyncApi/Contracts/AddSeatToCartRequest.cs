namespace TicketingSystem.AsyncApi.Contracts;

public class AddSeatToCartRequest
{
    public int EventId { get; set; }
    public int SeatId { get; set; }
    public int PriceId { get; set; }
}