using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TicketingSystem.AsyncApi.Contracts;
using TicketingSystem.AsyncApi.Contracts.Responses;
using TicketingSystem.DAL.EF;

namespace TicketingSystem.AsyncApi.Controllers;

[ApiController]
[Route("venues")]
public class VenuesController(TicketingDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<VenueResponse>>> GetVenuesAsync()
    {
        List<VenueResponse> venues = await dbContext.Venues
            .AsNoTracking()
            .Select(v => new VenueResponse
            {
                Id = v.Id,
                Name = v.Name,
                Address = v.Address,
                SectionsCount = v.Sections.Count
            })
            .ToListAsync();

        return Ok(venues);
    }

    [HttpGet("{venueId:int}/sections")]
    public async Task<ActionResult<IReadOnlyCollection<VenueSectionResponse>>> GetVenueSectionsAsync(int venueId)
    {
        bool venueExists = await dbContext.Venues
            .AsNoTracking()
            .AnyAsync(v => v.Id == venueId);

        if (!venueExists)
            return NotFound(new ApiErrorResponse { Message = $"Venue {venueId} not found." });

        List<VenueSectionResponse> sections = await dbContext.Sections
            .AsNoTracking()
            .Where(s => s.VenueId == venueId)
            .Select(s => new VenueSectionResponse
            {
                Id = s.Id,
                VenueId = s.VenueId,
                Name = s.Name,
                RowCount = s.RowCount,
                SeatsPerRow = s.SeatsPerRow
            })
            .ToListAsync();

        return Ok(sections);
    }
}