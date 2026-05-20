using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.EF;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.AsyncApi;

public static class SeedData
{
    public static async Task InitializeAsync(TicketingDbContext dbContext)
    {
        await dbContext.Database.MigrateAsync();

        if (await dbContext.Venues.AnyAsync() || await dbContext.Events.AnyAsync())
            return;

        Venue venue = new()
        {
            Name = "City Hall Arena",
            Address = "221 Main Street"
        };

        Section sectionA = new()
        {
            Name = "A",
            RowCount = 2,
            SeatsPerRow = 4,
            Venue = venue
        };

        Section sectionB = new()
        {
            Name = "B",
            RowCount = 2,
            SeatsPerRow = 4,
            Venue = venue
        };

        List<Seat> seats = [];
        CreateSeats(sectionA, seats);
        CreateSeats(sectionB, seats);

        Event eventEntity = new()
        {
            Venue = venue,
            Title = "Symphonic Evening",
            Description = "Classical music gala",
            Date = DateTime.UtcNow.AddDays(10)
        };

        await dbContext.Venues.AddAsync(venue);
        await dbContext.Sections.AddRangeAsync(sectionA, sectionB);
        await dbContext.Seats.AddRangeAsync(seats);
        await dbContext.Events.AddAsync(eventEntity);
        await dbContext.SaveChangesAsync();

        IEnumerable<EventSeat> eventSeats = seats.Select((seat, idx) => new EventSeat
        {
            EventId = eventEntity.Id,
            SeatId = seat.Id,
            Price = idx < seats.Count / 2 ? 50m : 35m,
            Status = SeatStatus.Available
        });

        await dbContext.EventSeats.AddRangeAsync(eventSeats);
        await dbContext.SaveChangesAsync();
    }

    private static void CreateSeats(Section section, List<Seat> seats)
    {
        for (int row = 1; row <= section.RowCount; row++)
        {
            for (int number = 1; number <= section.SeatsPerRow; number++)
            {
                seats.Add(new Seat()
                {
                    Section = section,
                    Row = row,
                    Number = number
                });
            }
        }
    }
}