using TicketingSystem.Domain.Entities;

namespace TicketingSystem.DAL.Interfaces;

public interface IPaymentRepository
{
    Task<Payment?> GetByIdAsync(Guid paymentId);
    Task<Payment?> GetWithOrderAsync(Guid paymentId);
    Task AddAsync(Payment payment);
}