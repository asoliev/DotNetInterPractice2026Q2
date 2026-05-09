using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.DAL.EF.Repositories;

public class EventSeatRepository(TicketingDbContext context) : Repository<EventSeat>(context), IEventSeatRepository
{
    public async Task<IEnumerable<EventSeat>> GetByEventIdAsync(int eventId) =>
        await DbSet
            .Include(es => es.Seat).ThenInclude(s => s.Section)
            .Where(es => es.EventId == eventId)
            .ToListAsync();

    public async Task<IEnumerable<EventSeat>> GetAvailableByEventIdAsync(int eventId) =>
        await DbSet
            .Include(es => es.Seat).ThenInclude(s => s.Section)
            .Where(es => es.EventId == eventId && es.Status == SeatStatus.Available)
            .ToListAsync();

    public async Task<EventSeat?> GetCheapestAvailableAsync(int eventId) =>
        await DbSet
            .Where(es => es.EventId == eventId && es.Status == SeatStatus.Available)
            .OrderBy(es => es.Price)
            .FirstOrDefaultAsync();

    public async Task<bool> TryChangeStatusAsync(int eventSeatId, SeatStatus expectedStatus, SeatStatus newStatus)
    {
        EventSeat? eventSeat = await DbSet.FindAsync(eventSeatId);
        if (eventSeat is null || eventSeat.Status != expectedStatus)
            return false;

        eventSeat.Status = newStatus;
        return true;
    }
}
