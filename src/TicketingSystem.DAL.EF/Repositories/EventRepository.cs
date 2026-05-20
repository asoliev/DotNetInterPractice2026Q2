using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.Exceptions;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.DAL.EF.Repositories;

public class EventRepository(TicketingDbContext context) : Repository<Event>(context), IEventRepository
{
    public async Task<IEnumerable<Event>> GetUpcomingAsync() =>
        await DbSet
            .Include(e => e.Venue)
            .Where(e => e.Date >= DateTime.UtcNow)
            .OrderBy(e => e.Date)
            .ToListAsync();

    public async Task<Event?> GetWithSeatsAsync(int eventId) =>
        await DbSet
            .Include(e => e.EventSeats).ThenInclude(es => es.Seat).ThenInclude(s => s.Section)
            .FirstOrDefaultAsync(e => e.Id == eventId);

    public async Task DeleteEventAsync(int eventId)
    {
        Event? eventEntity = await DbSet
            .Include(e => e.EventSeats)
            .FirstOrDefaultAsync(e => e.Id == eventId);

        if (eventEntity is null)
            throw new KeyNotFoundException($"Event {eventId} was not found.");

        if (eventEntity.EventSeats.Any(es => es.Status == SeatStatus.Sold))
            throw new SoldTicketDeletionNotAllowedException(eventId);

        DbSet.Remove(eventEntity);
    }
}
