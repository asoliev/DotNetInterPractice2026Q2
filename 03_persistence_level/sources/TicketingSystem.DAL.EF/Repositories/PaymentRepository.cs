using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.DAL.EF.Repositories;

public class PaymentRepository(TicketingDbContext context) : IPaymentRepository
{
    private readonly DbSet<Payment> _payments = context.Set<Payment>();

    public async Task<Payment?> GetByIdAsync(Guid paymentId) =>
        await _payments.FirstOrDefaultAsync(p => p.Id == paymentId);

    public async Task<Payment?> GetWithOrderAsync(Guid paymentId) =>
        await _payments
            .Include(p => p.Order)
                .ThenInclude(o => o.Items)
                .ThenInclude(i => i.EventSeat)
            .FirstOrDefaultAsync(p => p.Id == paymentId);

    public async Task AddAsync(Payment payment) =>
        await _payments.AddAsync(payment);
}