using System.Collections.Concurrent;

namespace TicketingSystem.AsyncApi.Services;

public class InMemoryCartStore : ICartStore
{
    private readonly ConcurrentDictionary<Guid, CartState> _carts = new();

    public CartState GetOrCreate(Guid cartId) =>
        _carts.GetOrAdd(cartId, static id => new CartState(id));

    public void AddItem(Guid cartId, int eventId, int seatId, int priceId)
    {
        CartState cart = GetOrCreate(cartId);
        lock (cart.SyncRoot)
        {
            bool exists = cart.Items.Any(i => i.EventId == eventId && i.SeatId == seatId);
            if (exists)
                return;

            cart.Items.Add(new CartItem(eventId, seatId, priceId));
        }
    }

    public bool RemoveItem(Guid cartId, int eventId, int seatId)
    {
        if (!_carts.TryGetValue(cartId, out CartState? cart))
            return false;

        lock (cart.SyncRoot)
        {
            CartItem? item = cart.Items.FirstOrDefault(i => i.EventId == eventId && i.SeatId == seatId);
            if (item is null)
                return false;

            cart.Items.Remove(item);
            return true;
        }
    }

    public void Clear(Guid cartId)
    {
        if (!_carts.TryGetValue(cartId, out CartState? cart)) return;
        lock (cart.SyncRoot)
        {
            cart.Items.Clear();
        }
    }
}

public class CartState(Guid id)
{
    public Guid Id { get; } = id;
    public List<CartItem> Items { get; } = [];
    public object SyncRoot { get; } = new();
}

public class CartItem(int eventId, int seatId, int priceId)
{
    public int EventId { get; } = eventId;
    public int SeatId { get; } = seatId;
    public int PriceId { get; } = priceId;
}