using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.DAL.EF.Repositories;

public class CartRepository(TicketingDbContext context) : ICartRepository
{
    private readonly DbSet<Cart> _carts = context.Set<Cart>();
    private readonly DbSet<CartItem> _cartItems = context.Set<CartItem>();

    public async Task<Cart?> GetByIdAsync(Guid cartId) =>
        await _carts.FirstOrDefaultAsync(c => c.Id == cartId);

    public async Task<Cart?> GetWithItemsAsync(Guid cartId) =>
        await _carts
            .Include(c => c.Items)
                .ThenInclude(i => i.EventSeat)
                .ThenInclude(es => es.Seat)
            .FirstOrDefaultAsync(c => c.Id == cartId);

    public async Task<CartItem?> GetItemAsync(Guid cartId, int eventId, int seatId) =>
        await _cartItems
            .Include(i => i.EventSeat)
            .FirstOrDefaultAsync(i => i.CartId == cartId && i.EventSeat.EventId == eventId && i.EventSeat.SeatId == seatId);

    public async Task AddAsync(Cart cart) =>
        await _carts.AddAsync(cart);

    public async Task AddItemAsync(CartItem item) =>
        await _cartItems.AddAsync(item);

    public void RemoveItem(CartItem item) =>
        _cartItems.Remove(item);

    public void RemoveItems(IEnumerable<CartItem> items) =>
        _cartItems.RemoveRange(items);
}