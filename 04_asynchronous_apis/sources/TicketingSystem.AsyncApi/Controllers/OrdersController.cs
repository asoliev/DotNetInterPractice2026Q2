using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketingSystem.AsyncApi.Contracts;
using TicketingSystem.AsyncApi.Services;
using TicketingSystem.DAL.EF;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.AsyncApi.Controllers;

[ApiController]
[Route("orders/carts/{cartId:guid}")]
public class OrdersController(
    IUnitOfWork unitOfWork,
    ICartStore cartStore,
    IPaymentStore paymentStore,
    TicketingDbContext dbContext) : ControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> GetCartAsync(Guid cartId)
    {
        CartState cart = cartStore.GetOrCreate(cartId);
        object state = await BuildCartStateAsync(cartId, cart.Items);
        return Ok(state);
    }

    [HttpPost("")]
    public async Task<IActionResult> AddSeatToCartAsync(Guid cartId, [FromBody] AddSeatToCartRequest request)
    {
        EventSeat? eventSeat = await dbContext.EventSeats
            .AsNoTracking()
            .Include(es => es.Seat)
            .FirstOrDefaultAsync(es => es.EventId == request.EventId && es.SeatId == request.SeatId);

        if (eventSeat is null)
            return NotFound(new { message = "Seat for the event was not found." });

        if (eventSeat.Status != SeatStatus.Available)
            return Conflict(new { message = "Seat is not available." });

        if (request.PriceId <= 0)
            return BadRequest(new { message = "priceId must be greater than 0." });

        cartStore.AddItem(cartId, request.EventId, request.SeatId, request.PriceId);

        CartState cart = cartStore.GetOrCreate(cartId);
        object state = await BuildCartStateAsync(cartId, cart.Items);
        return Ok(state);
    }

    [HttpDelete("events/{eventId:int}/seats/{seatId:int}")]
    public async Task<IActionResult> RemoveSeatFromCartAsync(Guid cartId, int eventId, int seatId)
    {
        bool removed = cartStore.RemoveItem(cartId, eventId, seatId);
        if (!removed)
            return NotFound(new { message = "Item not found in cart." });

        CartState cart = cartStore.GetOrCreate(cartId);
        object state = await BuildCartStateAsync(cartId, cart.Items);
        return Ok(state);
    }

    [HttpPut("book")]
    public async Task<IActionResult> BookCartAsync(Guid cartId)
    {
        CartState cart = cartStore.GetOrCreate(cartId);
        if (cart.Items.Count == 0)
            return BadRequest(new { message = "Cart is empty." });

        var distinctEventSeatKeys = cart.Items
            .Select(i => new { i.EventId, i.SeatId })
            .Distinct()
            .ToList();

        foreach (var key in distinctEventSeatKeys)
        {
            EventSeat? eventSeat = await dbContext.EventSeats
                .AsNoTracking()
                .FirstOrDefaultAsync(es => es.EventId == key.EventId && es.SeatId == key.SeatId);

            if (eventSeat is null)
                return NotFound(new { message = $"Seat {key.SeatId} for event {key.EventId} was not found." });

            if (eventSeat.Status != SeatStatus.Available)
                return Conflict(new { message = $"Seat {key.SeatId} for event {key.EventId} is not available." });
        }

        await unitOfWork.BeginTransactionAsync();
        try
        {
            Customer customer = await GetOrCreateCustomerForCartAsync(cartId);

            Order order = new()
            {
                CustomerId = customer.Id,
                CreatedAt = DateTime.UtcNow,
                Status = OrderStatus.Pending
            };

            await unitOfWork.Orders.AddAsync(order);
            await unitOfWork.SaveChangesAsync();

            List<int> bookedSeatIds = [];
            foreach (CartItem cartItem in cart.Items)
            {
                EventSeat? eventSeat = await dbContext.EventSeats
                    .FirstOrDefaultAsync(es => es.EventId == cartItem.EventId && es.SeatId == cartItem.SeatId);

                if (eventSeat is null)
                    throw new InvalidOperationException($"Seat {cartItem.SeatId} for event {cartItem.EventId} was not found during booking.");

                eventSeat.Status = SeatStatus.Booked;
                bookedSeatIds.Add(eventSeat.Id);

                await dbContext.OrderItems.AddAsync(new()
                {
                    OrderId = order.Id,
                    EventSeatId = eventSeat.Id,
                    PriceAtPurchase = eventSeat.Price
                });
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            Guid paymentId = paymentStore.CreatePayment(order.Id, bookedSeatIds);
            cartStore.Clear(cartId);

            return Ok(new { paymentId });
        }
        catch
        {
            try
            {
                await unitOfWork.RollbackTransactionAsync();
            }
            catch
            {
                // Preserve the original exception if rollback fails.
            }

            throw;
        }
    }

    private async Task<object> BuildCartStateAsync(Guid cartId, IReadOnlyCollection<CartItem> items)
    {
        List<object> itemResponses = [];
        decimal totalAmount = 0m;

        foreach (CartItem item in items)
        {
            EventSeat? eventSeat = await dbContext.EventSeats
                .AsNoTracking()
                .Include(es => es.Seat)
                .FirstOrDefaultAsync(es => es.EventId == item.EventId && es.SeatId == item.SeatId);

            if (eventSeat is null)
                continue;

            totalAmount += eventSeat.Price;
            itemResponses.Add(new
            {
                eventId = item.EventId,
                seatId = item.SeatId,
                priceId = item.PriceId,
                rowId = eventSeat.Seat.Row,
                amount = eventSeat.Price
            });
        }

        return new
        {
            cartId,
            items = itemResponses,
            totalAmount
        };
    }

    private async Task<Customer> GetOrCreateCustomerForCartAsync(Guid cartId)
    {
        string email = $"cart-{cartId:N}@example.local";
        Customer? existing = await unitOfWork.Customers.GetByEmailAsync(email);
        if (existing is not null)
            return existing;

        Customer customer = new()
        {
            Name = $"Cart {cartId:N}",
            Email = email
        };

        await unitOfWork.Customers.AddAsync(customer);
        await unitOfWork.SaveChangesAsync();
        return customer;
    }
}