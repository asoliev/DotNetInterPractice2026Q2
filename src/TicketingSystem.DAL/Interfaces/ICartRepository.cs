using TicketingSystem.Domain.Entities;

namespace TicketingSystem.DAL.Interfaces;

public interface ICartRepository
{
    Task<Cart?> GetByIdAsync(Guid cartId);
    Task<Cart?> GetWithItemsAsync(Guid cartId);
    Task<CartItem?> GetItemAsync(Guid cartId, int eventId, int seatId);
    Task AddAsync(Cart cart);
    Task AddItemAsync(CartItem item);
    void RemoveItem(CartItem item);
    void RemoveItems(IEnumerable<CartItem> items);
}