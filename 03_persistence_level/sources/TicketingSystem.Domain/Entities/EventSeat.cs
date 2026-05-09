using TicketingSystem.Domain.Enums;

namespace TicketingSystem.Domain.Entities;

public class EventSeat
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public int SeatId { get; set; }
    public decimal Price { get; set; }
    public SeatStatus Status { get; set; } = SeatStatus.Available;

    public Event Event { get; set; } = null!;
    public Seat Seat { get; set; } = null!;
    public OrderItem? OrderItem { get; set; }
}
