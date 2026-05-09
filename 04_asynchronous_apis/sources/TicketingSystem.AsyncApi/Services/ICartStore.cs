namespace TicketingSystem.AsyncApi.Services;

public interface ICartStore
{
    CartState GetOrCreate(Guid cartId);
    void AddItem(Guid cartId, int eventId, int seatId, int priceId);
    bool RemoveItem(Guid cartId, int eventId, int seatId);
    void Clear(Guid cartId);
}