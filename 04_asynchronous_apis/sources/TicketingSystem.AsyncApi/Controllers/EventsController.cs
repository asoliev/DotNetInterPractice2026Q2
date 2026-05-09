using Microsoft.AspNetCore.Mvc;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.AsyncApi.Controllers;

[ApiController]
[Route("events")]
public class EventsController(IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetEventsAsync()
    {
        IEnumerable<Event> events = await unitOfWork.Events.GetAllAsync();

        var response = events
            .OrderBy(e => e.Date)
            .Select(e => new
            {
                id = e.Id,
                venueId = e.VenueId,
                title = e.Title,
                description = e.Description,
                date = e.Date
            });

        return Ok(response);
    }

    [HttpGet("{eventId:int}/sections/{sectionId:int}/seats")]
    public async Task<IActionResult> GetSectionSeatsAsync(int eventId, int sectionId)
    {
        Event? eventEntity = await unitOfWork.Events.GetByIdAsync(eventId);
        if (eventEntity is null)
            return NotFound(new { message = $"Event {eventId} not found." });

        IEnumerable<EventSeat> eventSeats = await unitOfWork.EventSeats.GetByEventIdAsync(eventId);

        var seats = eventSeats
            .Where(es => es.Seat.SectionId == sectionId)
            .Select(es => new
            {
                sectionId,
                rowId = es.Seat.Row,
                seatId = es.SeatId,
                status = new
                {
                    id = (int)es.Status,
                    name = es.Status.ToString()
                },
                priceOptions = new[]
                {
                    new
                    {
                        id = 1,
                        name = "Standard",
                        amount = es.Price
                    }
                }
            })
            .ToList();

        return Ok(seats);
    }
}