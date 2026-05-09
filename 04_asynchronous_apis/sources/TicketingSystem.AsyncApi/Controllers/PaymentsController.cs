using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketingSystem.AsyncApi.Services;
using TicketingSystem.DAL.EF;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.AsyncApi.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController(IPaymentStore paymentStore, TicketingDbContext dbContext) : ControllerBase
{
    [HttpGet("{paymentId:guid}")]
    public IActionResult GetPaymentAsync(Guid paymentId)
    {
        var payment = paymentStore.Get(paymentId);
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
        var payment = paymentStore.Get(paymentId);
        if (payment is null)
            return NotFound(new { message = $"Payment {paymentId} not found." });

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (int eventSeatId in payment.EventSeatIds)
            {
                var eventSeat = await dbContext.EventSeats.FirstOrDefaultAsync(es => es.Id == eventSeatId);
                if (eventSeat is null)
                    return NotFound(new { message = $"EventSeat {eventSeatId} not found." });

                eventSeat.Status = SeatStatus.Sold;
            }

            var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
            if (order is not null)
                order.Status = OrderStatus.Confirmed;

            paymentStore.UpdateStatus(paymentId, PaymentStatus.Completed);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                paymentId = payment.Id,
                status = PaymentStatus.Completed.ToString()
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
        var payment = paymentStore.Get(paymentId);
        if (payment is null)
            return NotFound(new { message = $"Payment {paymentId} not found." });

        await using var transaction = await dbContext.Database.BeginTransactionAsync();
        try
        {
            foreach (int eventSeatId in payment.EventSeatIds)
            {
                var eventSeat = await dbContext.EventSeats.FirstOrDefaultAsync(es => es.Id == eventSeatId);
                if (eventSeat is null)
                    return NotFound(new { message = $"EventSeat {eventSeatId} not found." });

                eventSeat.Status = SeatStatus.Available;
            }

            var order = await dbContext.Orders.FirstOrDefaultAsync(o => o.Id == payment.OrderId);
            if (order is not null)
                order.Status = OrderStatus.Cancelled;

            paymentStore.UpdateStatus(paymentId, PaymentStatus.Failed);
            await dbContext.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                paymentId = payment.Id,
                status = PaymentStatus.Failed.ToString()
            });
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
}