using System.Collections.Concurrent;

namespace TicketingSystem.AsyncApi.Services;

public class InMemoryPaymentStore : IPaymentStore
{
    private readonly ConcurrentDictionary<Guid, PaymentRecord> _payments = new();

    public Guid CreatePayment(int orderId, IReadOnlyCollection<int> eventSeatIds)
    {
        var id = Guid.NewGuid();
        PaymentRecord record = new(id, orderId, eventSeatIds.ToArray(), PaymentStatus.Pending);
        _payments[id] = record;
        return id;
    }

    public PaymentRecord? Get(Guid paymentId)
    {
        _payments.TryGetValue(paymentId, out PaymentRecord? payment);
        return payment;
    }

    public void UpdateStatus(Guid paymentId, PaymentStatus status)
    {
        if (_payments.TryGetValue(paymentId, out PaymentRecord? payment))
        {
            _payments[paymentId] = payment with { Status = status };
        }
    }
}

public record PaymentRecord(Guid Id, int OrderId, IReadOnlyCollection<int> EventSeatIds, PaymentStatus Status);