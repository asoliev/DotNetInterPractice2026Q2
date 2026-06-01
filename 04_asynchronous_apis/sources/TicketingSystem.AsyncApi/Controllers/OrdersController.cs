using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketingSystem.AsyncApi.Caching;
using TicketingSystem.AsyncApi.Contracts;
using TicketingSystem.AsyncApi.Contracts.Responses;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.AsyncApi.Controllers;

[ApiController]
[Route("orders/carts/{cartId:guid}")]
public class OrdersController(
    IUnitOfWork unitOfWork,
    IEventResourceCache eventResourceCache) : ControllerBase
{
    [HttpGet("")]
    public async Task<ActionResult<CartResponse>> GetCartAsync(Guid cartId)
    {
        Cart cart = await GetOrCreateCartAsync(cartId);
        return Ok(BuildCartState(cart));
    }

    [HttpPost("")]
    public async Task<ActionResult<CartResponse>> AddSeatToCartAsync(Guid cartId, [FromBody] AddSeatToCartRequest request)
    {
        if (request.PriceId <= 0)
            return BadRequest(new ApiErrorResponse { Message = "priceId must be greater than 0." });

        await unitOfWork.BeginTransactionAsync();
        try
        {
            EventSeat? eventSeat = await unitOfWork.EventSeats.GetByEventAndSeatAsync(request.EventId, request.SeatId);

            if (eventSeat is null)
            {
                await unitOfWork.RollbackTransactionAsync();
                return NotFound(new ApiErrorResponse { Message = "Seat for the event was not found." });
            }

            if (eventSeat.Status != SeatStatus.Available)
            {
                await unitOfWork.RollbackTransactionAsync();
                return Conflict(new ApiErrorResponse { Message = "Seat is not available." });
            }

            eventSeat.Status = SeatStatus.Booked;

            Cart cart = await GetOrCreateCartAsync(cartId);
            CartItem? existingItem = await unitOfWork.Carts.GetItemAsync(cartId, request.EventId, request.SeatId);
            if (existingItem is null)
            {
                await unitOfWork.Carts.AddItemAsync(new CartItem
                {
                    CartId = cartId,
                    EventSeatId = eventSeat.Id,
                    PriceId = request.PriceId
                });
                cart.UpdatedAt = DateTime.UtcNow;
            }

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();
            eventResourceCache.Invalidate();

            Cart? persistedCart = await unitOfWork.Carts.GetWithItemsAsync(cartId);
            if (persistedCart is null)
                return NotFound(new ApiErrorResponse { Message = $"Cart {cartId} not found." });

            CartResponse state = BuildCartState(persistedCart);
            return Ok(state);
        }
        catch (DbUpdateConcurrencyException)
        {
            try
            {
                await unitOfWork.RollbackTransactionAsync();
            }
            catch
            {
                // Preserve the original exception if rollback fails.
            }

            return Conflict(new ApiErrorResponse { Message = "Seat is not available." });
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

    [HttpDelete("events/{eventId:int}/seats/{seatId:int}")]
    public async Task<ActionResult<CartResponse>> RemoveSeatFromCartAsync(Guid cartId, int eventId, int seatId)
    {
        Cart? cart = await unitOfWork.Carts.GetWithItemsAsync(cartId);
        if (cart is null)
            return NotFound(new ApiErrorResponse { Message = "Cart was not found." });

        CartItem? cartItem = await unitOfWork.Carts.GetItemAsync(cartId, eventId, seatId);
        if (cartItem is null)
            return NotFound(new ApiErrorResponse { Message = "Item not found in cart." });

        unitOfWork.Carts.RemoveItem(cartItem);
        cart.UpdatedAt = DateTime.UtcNow;
        await unitOfWork.SaveChangesAsync();

        Cart? updatedCart = await unitOfWork.Carts.GetWithItemsAsync(cartId);
        if (updatedCart is null)
            return NotFound(new ApiErrorResponse { Message = "Cart was not found." });

        CartResponse state = BuildCartState(updatedCart);
        return Ok(state);
    }

    [HttpPut("book")]
    public async Task<ActionResult<BookCartResponse>> BookCartAsync(Guid cartId)
    {
        Cart? cart = await unitOfWork.Carts.GetWithItemsAsync(cartId);
        if (cart is null)
            return NotFound(new ApiErrorResponse { Message = "Cart was not found." });

        if (cart.Items.Count == 0)
            return BadRequest(new ApiErrorResponse { Message = "Cart is empty." });

        foreach (CartItem cartItem in cart.Items)
        {
            if (cartItem.EventSeat.Status != SeatStatus.Booked)
                return Conflict(new ApiErrorResponse
                {
                    Message = $"Seat {cartItem.EventSeat.SeatId} for event {cartItem.EventSeat.EventId} is not booked."
                });
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

            foreach (CartItem cartItem in cart.Items)
            {
                order.Items.Add(new OrderItem
                {
                    EventSeatId = cartItem.EventSeatId,
                    PriceAtPurchase = cartItem.EventSeat.Price
                });
            }

            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                Status = PaymentStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            await unitOfWork.Payments.AddAsync(payment);

            unitOfWork.Carts.RemoveItems(cart.Items);
            cart.UpdatedAt = DateTime.UtcNow;

            await unitOfWork.SaveChangesAsync();
            await unitOfWork.CommitTransactionAsync();

            eventResourceCache.Invalidate();

            return Ok(new BookCartResponse { PaymentId = payment.Id });
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

    private static CartResponse BuildCartState(Cart cart)
    {
        List<CartItemResponse> itemResponses = [];
        decimal totalAmount = 0m;

        foreach (CartItem item in cart.Items)
        {
            totalAmount += item.EventSeat.Price;
            itemResponses.Add(new CartItemResponse
            {
                EventId = item.EventSeat.EventId,
                SeatId = item.EventSeat.SeatId,
                PriceId = item.PriceId,
                RowId = item.EventSeat.Seat.Row,
                Amount = item.EventSeat.Price
            });
        }

        return new CartResponse
        {
            CartId = cart.Id,
            Items = itemResponses,
            TotalAmount = totalAmount
        };
    }

    private async Task<Cart> GetOrCreateCartAsync(Guid cartId)
    {
        Cart? cart = await unitOfWork.Carts.GetWithItemsAsync(cartId);
        if (cart is not null)
            return cart;

        cart = new Cart
        {
            Id = cartId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await unitOfWork.Carts.AddAsync(cart);
        await unitOfWork.SaveChangesAsync();
        return cart;
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