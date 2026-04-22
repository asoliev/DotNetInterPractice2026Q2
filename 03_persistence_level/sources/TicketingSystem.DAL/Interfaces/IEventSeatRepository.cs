using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.DAL.Interfaces;

public interface IEventSeatRepository : IRepository<EventSeat>
{
    Task<IEnumerable<EventSeat>> GetByEventIdAsync(int eventId);
    Task<IEnumerable<EventSeat>> GetAvailableByEventIdAsync(int eventId);
    Task<EventSeat?> GetCheapestAvailableAsync(int eventId);
    Task<bool> TryChangeStatusAsync(int eventSeatId, SeatStatus expectedStatus, SeatStatus newStatus);
}
