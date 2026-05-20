namespace TicketingSystem.Domain.Entities;

public class CartItem
{
    public int Id { get; set; }
    public Guid CartId { get; set; }
    public int EventSeatId { get; set; }
    public int PriceId { get; set; }

    public Cart Cart { get; set; } = null!;
    public EventSeat EventSeat { get; set; } = null!;
}