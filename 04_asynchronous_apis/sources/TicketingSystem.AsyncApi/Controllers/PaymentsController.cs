using Microsoft.AspNetCore.Mvc;
using TicketingSystem.AsyncApi.Contracts;
using TicketingSystem.AsyncApi.Contracts.Responses;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.AsyncApi.Controllers;

[ApiController]
[Route("payments")]
public class PaymentsController(IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet("{paymentId:guid}")]
    public async Task<ActionResult<PaymentResponse>> GetPaymentAsync(Guid paymentId)
    {
        Payment? payment = await unitOfWork.Payments.GetByIdAsync(paymentId);
        if (payment is null)
            return NotFound(new ApiErrorResponse { Message = $"Payment {paymentId} not found." });

        return Ok(new PaymentResponse
        {
            PaymentId = payment.Id,
            OrderId = payment.OrderId,
            Status = payment.Status.ToString()
        });
    }

    [HttpPost("{paymentId:guid}/complete")]
    public async Task<ActionResult<PaymentStatusUpdateResponse>> CompletePaymentAsync(Guid paymentId)
    {
        Payment? payment = await unitOfWork.Payments.GetWithOrderAsync(paymentId);
        if (payment is null)
            return NotFound(new ApiErrorResponse { Message = $"Payment {paymentId} not found." });

        if (payment.Status == PaymentStatus.Completed)
        {
            return Ok(new PaymentStatusUpdateResponse
            {
                PaymentId = payment.Id,
                Status = nameof(PaymentStatus.Completed)
            });
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (OrderItem orderItem in payment.Order.Items)
            {
                orderItem.EventSeat.Status = SeatStatus.Sold;
            }

            payment.Order.Status = OrderStatus.Confirmed;
            payment.Status = PaymentStatus.Completed;

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            return Ok(new PaymentStatusUpdateResponse
            {
                PaymentId = payment.Id,
                Status = nameof(PaymentStatus.Completed)
            });
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    [HttpPost("{paymentId:guid}/failed")]
    public async Task<ActionResult<PaymentStatusUpdateResponse>> FailPaymentAsync(Guid paymentId)
    {
        Payment? payment = await unitOfWork.Payments.GetWithOrderAsync(paymentId);
        if (payment is null)
            return NotFound(new ApiErrorResponse { Message = $"Payment {paymentId} not found." });

        if (payment.Status == PaymentStatus.Failed)
        {
            return Ok(new PaymentStatusUpdateResponse
            {
                PaymentId = payment.Id,
                Status = nameof(PaymentStatus.Failed)
            });
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            foreach (OrderItem orderItem in payment.Order.Items)
            {
                orderItem.EventSeat.Status = SeatStatus.Available;
            }

            payment.Order.Status = OrderStatus.Cancelled;
            payment.Status = PaymentStatus.Failed;

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            return Ok(new PaymentStatusUpdateResponse
            {
                PaymentId = payment.Id,
                Status = nameof(PaymentStatus.Failed)
            });
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}