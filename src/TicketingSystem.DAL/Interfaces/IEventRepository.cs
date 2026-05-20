using TicketingSystem.Domain.Entities;

namespace TicketingSystem.DAL.Interfaces;

public interface IEventRepository : IRepository<Event>
{
    Task<IEnumerable<Event>> GetUpcomingAsync();
    Task<Event?> GetWithSeatsAsync(int eventId);
    Task DeleteEventAsync(int eventId);
}
