using Microsoft.AspNetCore.Mvc;
using Moq;
using TicketingSystem.AsyncApi.Contracts.Responses;
using TicketingSystem.AsyncApi.Controllers;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.UnitTests.Controllers;

public class PaymentsControllerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IPaymentRepository> _paymentRepoMock = new();

    public PaymentsControllerTests()
    {
        _unitOfWorkMock.Setup(u => u.Payments).Returns(_paymentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
    }

    private PaymentsController CreateController() => new(_unitOfWorkMock.Object);

    // ── GetPaymentAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetPaymentAsync_ReturnsNotFound_WhenPaymentDoesNotExist()
    {
        var paymentId = Guid.NewGuid();
        _paymentRepoMock.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync((Payment?)null);

        var result = await CreateController().GetPaymentAsync(paymentId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetPaymentAsync_ReturnsOk_WithPaymentDetails()
    {
        var paymentId = Guid.NewGuid();
        var payment = new Payment { Id = paymentId, OrderId = 1, Status = PaymentStatus.Pending };
        _paymentRepoMock.Setup(r => r.GetByIdAsync(paymentId)).ReturnsAsync(payment);

        var result = await CreateController().GetPaymentAsync(paymentId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PaymentResponse>(ok.Value);
        Assert.Equal(paymentId, response.PaymentId);
        Assert.Equal("Pending", response.Status);
    }

    // ── CompletePaymentAsync ─────────────────────────────────────────────────

    [Fact]
    public async Task CompletePaymentAsync_ReturnsNotFound_WhenPaymentDoesNotExist()
    {
        var paymentId = Guid.NewGuid();
        _paymentRepoMock.Setup(r => r.GetWithOrderAsync(paymentId)).ReturnsAsync((Payment?)null);

        var result = await CreateController().CompletePaymentAsync(paymentId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task CompletePaymentAsync_ReturnsOk_WhenAlreadyCompleted()
    {
        var paymentId = Guid.NewGuid();
        var payment = new Payment { Id = paymentId, Status = PaymentStatus.Completed, Order = new Order() };
        _paymentRepoMock.Setup(r => r.GetWithOrderAsync(paymentId)).ReturnsAsync(payment);

        var result = await CreateController().CompletePaymentAsync(paymentId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PaymentStatusUpdateResponse>(ok.Value);
        Assert.Equal("Completed", response.Status);
        // No transaction was started for idempotent call
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task CompletePaymentAsync_MarksSeatsAsSold_AndConfirmsOrder()
    {
        var paymentId = Guid.NewGuid();
        var seat = new EventSeat { Id = 1, Status = SeatStatus.Booked };
        var orderItem = new OrderItem { EventSeat = seat };
        var order = new Order { Status = OrderStatus.Pending, Items = new List<OrderItem> { orderItem } };
        var payment = new Payment { Id = paymentId, Status = PaymentStatus.Pending, Order = order };

        _paymentRepoMock.Setup(r => r.GetWithOrderAsync(paymentId)).ReturnsAsync(payment);

        var result = await CreateController().CompletePaymentAsync(paymentId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(SeatStatus.Sold, seat.Status);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(PaymentStatus.Completed, payment.Status);
    }

    // ── FailPaymentAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task FailPaymentAsync_ReturnsNotFound_WhenPaymentDoesNotExist()
    {
        var paymentId = Guid.NewGuid();
        _paymentRepoMock.Setup(r => r.GetWithOrderAsync(paymentId)).ReturnsAsync((Payment?)null);

        var result = await CreateController().FailPaymentAsync(paymentId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task FailPaymentAsync_ReturnsOk_WhenAlreadyFailed()
    {
        var paymentId = Guid.NewGuid();
        var payment = new Payment { Id = paymentId, Status = PaymentStatus.Failed, Order = new Order() };
        _paymentRepoMock.Setup(r => r.GetWithOrderAsync(paymentId)).ReturnsAsync(payment);

        var result = await CreateController().FailPaymentAsync(paymentId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<PaymentStatusUpdateResponse>(ok.Value);
        Assert.Equal("Failed", response.Status);
        _unitOfWorkMock.Verify(u => u.BeginTransactionAsync(), Times.Never);
    }

    [Fact]
    public async Task FailPaymentAsync_ReleasesSeats_AndCancelsOrder()
    {
        var paymentId = Guid.NewGuid();
        var seat = new EventSeat { Id = 1, Status = SeatStatus.Booked };
        var orderItem = new OrderItem { EventSeat = seat };
        var order = new Order { Status = OrderStatus.Pending, Items = new List<OrderItem> { orderItem } };
        var payment = new Payment { Id = paymentId, Status = PaymentStatus.Pending, Order = order };

        _paymentRepoMock.Setup(r => r.GetWithOrderAsync(paymentId)).ReturnsAsync(payment);

        var result = await CreateController().FailPaymentAsync(paymentId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(SeatStatus.Available, seat.Status);
        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Equal(PaymentStatus.Failed, payment.Status);
    }
}
