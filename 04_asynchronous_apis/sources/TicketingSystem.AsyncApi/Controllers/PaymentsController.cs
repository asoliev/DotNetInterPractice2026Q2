using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TicketingSystem.AsyncApi.Services;
using TicketingSystem.DAL.EF;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.AsyncApi.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController(IPaymentStore paymentStore, TicketingDbContext dbContext) : ControllerBase
{
    [HttpGet("{paymentId:guid}")]
    public IActionResult GetPaymentAsync(Guid paymentId)
    {
        PaymentRecord? payment = paymentStore.Get(paymentId);
        if (payment is null)
            return NotFound(new { message = $"Payment {paymentId} not found." });

        return Ok(new
        {
            paymentId = payment.Id,
            orderId = payment.OrderId,
            status = payment.Status.ToString()
        });
    }

    [HttpPost("{paymentId:guid}/complete")]
    public async Task<IActionResult> CompletePaymentAsync(Guid paymentId)
    {
        PaymentRecord? payment = paymentStore.Get(paymentId);
        if (payment is null)
            return NotFound(new { message = $"Payment {paymentId} not found." });

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (int eventSeatId in payment.EventSeatIds)
            {
                EventSeat? eventSeat = await dbContext.EventSeats.FirstOrDefaultAsync(es => es.Id == eventSeatId);
                if (eventSeat is null)
                    return NotFound(new { message = $"EventSeat {eventSeatId} not found." });

                eventSeat.Status = SeatStatus.Sold;
            }

            Order? order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
            order?.Status = OrderStatus.Confirmed;

            paymentStore.UpdateStatus(paymentId, PaymentStatus.Completed);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                paymentId = payment.Id,
                status = nameof(PaymentStatus.Completed)
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    [HttpPost("{paymentId:guid}/failed")]
    public async Task<IActionResult> FailPaymentAsync(Guid paymentId)
    {
        PaymentRecord? payment = paymentStore.Get(paymentId);
        if (payment is null)
            return NotFound(new { message = $"Payment {paymentId} not found." });

        await using IDbContextTransaction transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (int eventSeatId in payment.EventSeatIds)
            {
                EventSeat? eventSeat = await dbContext.EventSeats.FirstOrDefaultAsync(es => es.Id == eventSeatId);
                if (eventSeat is null)
                    return NotFound(new { message = $"EventSeat {eventSeatId} not found." });

                eventSeat.Status = SeatStatus.Available;
            }

            Order? order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
            order?.Status = OrderStatus.Cancelled;

            paymentStore.UpdateStatus(paymentId, PaymentStatus.Failed);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                paymentId = payment.Id,
                status = nameof(PaymentStatus.Failed)
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}