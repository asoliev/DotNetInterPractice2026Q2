namespace TicketingSystem.Domain.Entities;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int EventSeatId { get; set; }
    public decimal PriceAtPurchase { get; set; }

    public Order Order { get; set; } = null!;
    public EventSeat EventSeat { get; set; } = null!;
}
