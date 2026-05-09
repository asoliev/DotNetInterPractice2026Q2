namespace TicketingSystem.AsyncApi.Services;

public interface IPaymentStore
{
    Guid CreatePayment(int orderId, IReadOnlyCollection<int> eventSeatIds);
    PaymentRecord? Get(Guid paymentId);
    void UpdateStatus(Guid paymentId, PaymentStatus status);
}

public enum PaymentStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2
}