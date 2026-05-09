using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.EF;

namespace TicketingSystem.AsyncApi.Controllers;

[ApiController]
[Route("venues")]
public class VenuesController(TicketingDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetVenuesAsync()
    {
        var venues = await dbContext.Venues
            .AsNoTracking()
            .Select(v => new
            {
                id = v.Id,
                name = v.Name,
                address = v.Address,
                sectionsCount = v.Sections.Count
            })
            .ToListAsync();

        return Ok(venues);
    }

    [HttpGet("{venueId:int}/sections")]
    public async Task<IActionResult> GetVenueSectionsAsync(int venueId)
    {
        bool venueExists = await dbContext.Venues
            .AsNoTracking()
            .AnyAsync(v => v.Id == venueId);

        if (!venueExists)
            return NotFound(new { message = $"Venue {venueId} not found." });

        var sections = await dbContext.Sections
            .AsNoTracking()
            .Where(s => s.VenueId == venueId)
            .Select(s => new
            {
                id = s.Id,
                venueId = s.VenueId,
                name = s.Name,
                rowCount = s.RowCount,
                seatsPerRow = s.SeatsPerRow
            })
            .ToListAsync();

        return Ok(sections);
    }
}