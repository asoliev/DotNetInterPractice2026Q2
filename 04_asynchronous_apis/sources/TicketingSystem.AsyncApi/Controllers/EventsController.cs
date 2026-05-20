using Microsoft.AspNetCore.Mvc;
using TicketingSystem.AsyncApi.Caching;
using TicketingSystem.AsyncApi.Contracts;
using TicketingSystem.AsyncApi.Contracts.Responses;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.AsyncApi.Controllers;

[ApiController]
[Route("events")]
public class EventsController(
    IUnitOfWork unitOfWork,
    IEventResourceCache eventResourceCache) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<EventResponse>>> GetEventsAsync()
    {
        IReadOnlyCollection<EventResponse> response = await eventResourceCache.GetEventsAsync(async () =>
        {
            IEnumerable<Event> events = await unitOfWork.Events.GetAllAsync();

            return events
                .OrderBy(e => e.Date)
                .Select(e => new EventResponse
                {
                    Id = e.Id,
                    VenueId = e.VenueId,
                    Title = e.Title,
                    Description = e.Description,
                    Date = e.Date
                })
                .ToList();
        });

        return Ok(response);
    }

    [HttpGet("{eventId:int}/sections/{sectionId:int}/seats")]
    public async Task<ActionResult<IReadOnlyCollection<EventSeatResponse>>> GetSectionSeatsAsync(int eventId, int sectionId)
    {
        Event? eventEntity = await unitOfWork.Events.GetByIdAsync(eventId);
        if (eventEntity is null)
            return NotFound(new ApiErrorResponse { Message = $"Event {eventId} not found." });

        IReadOnlyCollection<EventSeatResponse> seats = await eventResourceCache.GetSectionSeatsAsync(eventId, sectionId,
            async () =>
            {
                IEnumerable<EventSeat> eventSeats = await unitOfWork.EventSeats.GetByEventIdAsync(eventId);

                return eventSeats
                    .Where(es => es.Seat.SectionId == sectionId)
                    .Select(es => new EventSeatResponse
                    {
                        SectionId = sectionId,
                        RowId = es.Seat.Row,
                        SeatId = es.SeatId,
                        Status = new SeatStatusResponse
                        {
                            Id = (int)es.Status,
                            Name = es.Status.ToString()
                        },
                        PriceOptions = new[]
                        {
                            new PriceOptionResponse
                            {
                                Id = 1,
                                Name = "Standard",
                                Amount = es.Price
                            }
                        }
                    })
                    .ToList();
            });

        return Ok(seats);
    }
}