using TicketingSystem.Domain.Enums;

namespace TicketingSystem.Domain.Entities;

public class Payment
{
    public Guid Id { get; set; }
    public int OrderId { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Order Order { get; set; } = null!;
}