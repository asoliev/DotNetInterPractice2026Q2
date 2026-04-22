using TicketingSystem.Domain.Enums;

namespace TicketingSystem.Domain.Entities;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public Customer Customer { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
