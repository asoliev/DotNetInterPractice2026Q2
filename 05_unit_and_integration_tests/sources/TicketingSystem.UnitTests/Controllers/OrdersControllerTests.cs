using Microsoft.AspNetCore.Mvc;
using Moq;
using TicketingSystem.AsyncApi.Caching;
using TicketingSystem.AsyncApi.Contracts;
using TicketingSystem.AsyncApi.Contracts.Responses;
using TicketingSystem.AsyncApi.Controllers;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.UnitTests.Controllers;

public class OrdersControllerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<ICartRepository> _cartRepoMock = new();
    private readonly Mock<IEventSeatRepository> _eventSeatRepoMock = new();
    private readonly Mock<IOrderRepository> _orderRepoMock = new();
    private readonly Mock<ICustomerRepository> _customerRepoMock = new();
    private readonly Mock<IPaymentRepository> _paymentRepoMock = new();
    private readonly Mock<IEventResourceCache> _eventResourceCacheMock = new();
    private readonly ISeatBookingGate _seatBookingGate = new SeatBookingGate();

    public OrdersControllerTests()
    {
        _unitOfWorkMock.Setup(u => u.Carts).Returns(_cartRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.EventSeats).Returns(_eventSeatRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Orders).Returns(_orderRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Customers).Returns(_customerRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.Payments).Returns(_paymentRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _unitOfWorkMock.Setup(u => u.BeginTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.CommitTransactionAsync()).Returns(Task.CompletedTask);
        _unitOfWorkMock.Setup(u => u.RollbackTransactionAsync()).Returns(Task.CompletedTask);
    }

    private OrdersController CreateController() => new(_unitOfWorkMock.Object, _eventResourceCacheMock.Object, _seatBookingGate);

    // ── GetCartAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCartAsync_ReturnsOk_WithExistingCart()
    {
        var cartId = Guid.NewGuid();
        var cart = new Cart { Id = cartId };
        _cartRepoMock.Setup(r => r.GetWithItemsAsync(cartId)).ReturnsAsync(cart);

        var result = await CreateController().GetCartAsync(cartId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<CartResponse>(ok.Value);
        Assert.Equal(cartId, response.CartId);
    }

    [Fact]
    public async Task GetCartAsync_CreatesCart_WhenNotFound()
    {
        var cartId = Guid.NewGuid();
        // First call (GetOrCreate) returns null → creates; subsequent call returns new cart
        var newCart = new Cart { Id = cartId };
        _cartRepoMock.SetupSequence(r => r.GetWithItemsAsync(cartId))
            .ReturnsAsync((Cart?)null)
            .ReturnsAsync(newCart);
        _cartRepoMock.Setup(r => r.AddAsync(It.IsAny<Cart>())).Returns(Task.CompletedTask);

        var result = await CreateController().GetCartAsync(cartId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        _cartRepoMock.Verify(r => r.AddAsync(It.Is<Cart>(c => c.Id == cartId)), Times.Once);
    }

    // ── AddSeatToCartAsync ───────────────────────────────────────────────────

    [Fact]
    public async Task AddSeatToCartAsync_ReturnsNotFound_WhenSeatNotFound()
    {
        var cartId = Guid.NewGuid();
        var request = new AddSeatToCartRequest { EventId = 1, SeatId = 1, PriceId = 1 };
        _eventSeatRepoMock.Setup(r => r.GetByEventAndSeatAsync(1, 1)).ReturnsAsync((EventSeat?)null);

        var result = await CreateController().AddSeatToCartAsync(cartId, request);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddSeatToCartAsync_ReturnsConflict_WhenSeatNotAvailable()
    {
        var cartId = Guid.NewGuid();
        var request = new AddSeatToCartRequest { EventId = 1, SeatId = 1, PriceId = 1 };
        var seat = new EventSeat { Id = 1, Status = SeatStatus.Booked };
        _eventSeatRepoMock.Setup(r => r.GetByEventAndSeatAsync(1, 1)).ReturnsAsync(seat);

        var result = await CreateController().AddSeatToCartAsync(cartId, request);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddSeatToCartAsync_ReturnsBadRequest_WhenPriceIdIsZero()
    {
        var cartId = Guid.NewGuid();
        var request = new AddSeatToCartRequest { EventId = 1, SeatId = 1, PriceId = 0 };
        var seat = new EventSeat { Id = 1, Status = SeatStatus.Available };
        _eventSeatRepoMock.Setup(r => r.GetByEventAndSeatAsync(1, 1)).ReturnsAsync(seat);

        var result = await CreateController().AddSeatToCartAsync(cartId, request);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task AddSeatToCartAsync_ReturnsOk_WhenSeatAddedSuccessfully()
    {
        var cartId = Guid.NewGuid();
        var request = new AddSeatToCartRequest { EventId = 1, SeatId = 5, PriceId = 1 };

        var seat = new EventSeat { Id = 10, EventId = 1, SeatId = 5, Status = SeatStatus.Available, Price = 99m,
            Seat = new Seat { Id = 5, Row = 1, SectionId = 1, Section = new Domain.Entities.Section { Id = 1, Name = "A" } } };

        _eventSeatRepoMock.Setup(r => r.GetByEventAndSeatAsync(1, 5)).ReturnsAsync(seat);

        var existingCart = new Cart { Id = cartId };
        _cartRepoMock.SetupSequence(r => r.GetWithItemsAsync(cartId))
            .ReturnsAsync(existingCart)       // GetOrCreate
            .ReturnsAsync(existingCart);      // after add

        _cartRepoMock.Setup(r => r.GetItemAsync(cartId, 1, 5)).ReturnsAsync((CartItem?)null);
        _cartRepoMock.Setup(r => r.AddItemAsync(It.IsAny<CartItem>())).Returns(Task.CompletedTask);

        var result = await CreateController().AddSeatToCartAsync(cartId, request);

        Assert.IsType<OkObjectResult>(result.Result);
        _cartRepoMock.Verify(r => r.AddItemAsync(It.IsAny<CartItem>()), Times.Once);
    }

    // ── RemoveSeatFromCartAsync ──────────────────────────────────────────────

    [Fact]
    public async Task RemoveSeatFromCartAsync_ReturnsNotFound_WhenCartNotFound()
    {
        var cartId = Guid.NewGuid();
        _cartRepoMock.Setup(r => r.GetWithItemsAsync(cartId)).ReturnsAsync((Cart?)null);

        var result = await CreateController().RemoveSeatFromCartAsync(cartId, 1, 1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task RemoveSeatFromCartAsync_ReturnsNotFound_WhenItemNotInCart()
    {
        var cartId = Guid.NewGuid();
        _cartRepoMock.Setup(r => r.GetWithItemsAsync(cartId)).ReturnsAsync(new Cart { Id = cartId });
        _cartRepoMock.Setup(r => r.GetItemAsync(cartId, 1, 1)).ReturnsAsync((CartItem?)null);

        var result = await CreateController().RemoveSeatFromCartAsync(cartId, 1, 1);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task RemoveSeatFromCartAsync_ReturnsOk_AfterRemoval()
    {
        var cartId = Guid.NewGuid();
        var cartItem = new CartItem
        {
            Id = 1, CartId = cartId, EventSeatId = 10, PriceId = 1,
            EventSeat = new EventSeat
            {
                Id = 10, EventId = 1, SeatId = 5, Price = 50m,
                Seat = new Seat { Id = 5, Row = 1, SectionId = 1, Section = new Domain.Entities.Section { Id = 1, Name = "A" } }
            }
        };

        var cart = new Cart { Id = cartId };
        var updatedCart = new Cart { Id = cartId }; // empty after removal

        _cartRepoMock.SetupSequence(r => r.GetWithItemsAsync(cartId))
            .ReturnsAsync(cart)
            .ReturnsAsync(updatedCart);

        _cartRepoMock.Setup(r => r.GetItemAsync(cartId, 1, 5)).ReturnsAsync(cartItem);
        _cartRepoMock.Setup(r => r.RemoveItem(cartItem));

        var result = await CreateController().RemoveSeatFromCartAsync(cartId, 1, 5);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.IsType<CartResponse>(ok.Value);
        _cartRepoMock.Verify(r => r.RemoveItem(cartItem), Times.Once);
    }

    // ── BookCartAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task BookCartAsync_ReturnsNotFound_WhenCartNotFound()
    {
        var cartId = Guid.NewGuid();
        _cartRepoMock.Setup(r => r.GetWithItemsAsync(cartId)).ReturnsAsync((Cart?)null);

        var result = await CreateController().BookCartAsync(cartId);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task BookCartAsync_ReturnsBadRequest_WhenCartIsEmpty()
    {
        var cartId = Guid.NewGuid();
        _cartRepoMock.Setup(r => r.GetWithItemsAsync(cartId)).ReturnsAsync(new Cart { Id = cartId });

        var result = await CreateController().BookCartAsync(cartId);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task BookCartAsync_ReturnsConflict_WhenSeatNotAvailable()
    {
        var cartId = Guid.NewGuid();
        var unavailableSeat = new EventSeat { Id = 1, Status = SeatStatus.Booked, EventId = 1, SeatId = 1 };
        var cart = new Cart
        {
            Id = cartId,
            Items = new List<CartItem>
            {
                new() { EventSeatId = 1, EventSeat = unavailableSeat }
            }
        };
        _cartRepoMock.Setup(r => r.GetWithItemsAsync(cartId)).ReturnsAsync(cart);

        var result = await CreateController().BookCartAsync(cartId);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task BookCartAsync_ReturnsOk_WithPaymentId_WhenSuccessful()
    {
        var cartId = Guid.NewGuid();
        var seat = new EventSeat { Id = 1, Status = SeatStatus.Available, EventId = 1, SeatId = 1, Price = 50m };
        var cart = new Cart
        {
            Id = cartId,
            Items = new List<CartItem>
            {
                new() { Id = 1, EventSeatId = 1, PriceId = 1, EventSeat = seat }
            }
        };

        _cartRepoMock.Setup(r => r.GetWithItemsAsync(cartId)).ReturnsAsync(cart);

        var customer = new Customer { Id = 1, Name = "Test", Email = $"cart-{cartId:N}@example.local" };
        _customerRepoMock.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync(customer);

        _orderRepoMock.Setup(r => r.AddAsync(It.IsAny<Order>())).Returns(Task.CompletedTask);
        _paymentRepoMock.Setup(r => r.AddAsync(It.IsAny<Payment>())).Returns(Task.CompletedTask);
        _cartRepoMock.Setup(r => r.RemoveItems(It.IsAny<IEnumerable<CartItem>>()));

        var result = await CreateController().BookCartAsync(cartId);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<BookCartResponse>(ok.Value);
        Assert.NotEqual(Guid.Empty, response.PaymentId);
    }
}
