using Microsoft.AspNetCore.Http.Headers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;
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
    public async Task<ActionResult<IReadOnlyCollection<EventResponse>>> GetEventsAsync(CancellationToken cancellationToken = default)
    {
        const string resourceKey = EventResourceCache.EventsListResourceKey;
        EventCacheMetadata metadata = eventResourceCache.GetMetadata(resourceKey);
        if (IsClientCacheValid(Request, metadata))
        {
            ApplyHttpCacheHeaders(Response, metadata);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        IReadOnlyCollection<EventResponse> response = await eventResourceCache.GetEventsAsync(async _ =>
        {
            IEnumerable<Event> events = await unitOfWork.Events.GetAllAsync();

            IReadOnlyCollection<EventResponse> value = [.. events
                .OrderBy(e => e.Date)
                .Select(e => new EventResponse
                {
                    Id = e.Id,
                    VenueId = e.VenueId,
                    Title = e.Title,
                    Description = e.Description,
                    Date = e.Date
                })];
            return value;
        }, cancellationToken);

        ApplyHttpCacheHeaders(Response, metadata);
        return Ok(response);
    }

    [HttpGet("{eventId:int}/sections/{sectionId:int}/seats")]
    public async Task<ActionResult<IReadOnlyCollection<EventSeatResponse>>> GetSectionSeatsAsync(int eventId, int sectionId, CancellationToken cancellationToken = default)
    {
        string resourceKey = EventResourceCache.BuildSectionSeatsResourceKey(eventId, sectionId);
        EventCacheMetadata metadata = eventResourceCache.GetMetadata(resourceKey);
        if (IsClientCacheValid(Request, metadata))
        {
            ApplyHttpCacheHeaders(Response, metadata);
            return StatusCode(StatusCodes.Status304NotModified);
        }

        Event? eventEntity = await unitOfWork.Events.GetByIdAsync(eventId);
        if (eventEntity is null)
            return NotFound(new ApiErrorResponse { Message = $"Event {eventId} not found." });

        IReadOnlyCollection<EventSeatResponse> seats = await eventResourceCache.GetSectionSeatsAsync(eventId, sectionId,
            async _ =>
            {
                IEnumerable<EventSeat> eventSeats = await unitOfWork.EventSeats.GetByEventIdAsync(eventId);

                IReadOnlyCollection<EventSeatResponse> value = [.. eventSeats
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
                    })];
                return value;
            }, cancellationToken);

        ApplyHttpCacheHeaders(Response, metadata);
        return Ok(seats);
    }

    private static bool IsClientCacheValid(HttpRequest request, EventCacheMetadata metadata)
    {
        RequestHeaders requestHeaders = request.GetTypedHeaders();

        if (requestHeaders.IfNoneMatch is { Count: > 0 })
        {
            bool etagMatches = requestHeaders.IfNoneMatch.Any(tag =>
                tag.Tag == "*" ||
                string.Equals(tag.Tag.Value, metadata.ETag.Tag.Value, StringComparison.Ordinal));

            if (etagMatches)
                return true;
        }

        if (requestHeaders.IfModifiedSince is DateTimeOffset ifModifiedSince)
        {
            if (metadata.LastModified <= ifModifiedSince)
                return true;
        }

        return false;
    }

    private static void ApplyHttpCacheHeaders(HttpResponse response, EventCacheMetadata metadata)
    {
        ResponseHeaders responseHeaders = response.GetTypedHeaders();
        responseHeaders.CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromSeconds(30),
            MustRevalidate = true
        };
        responseHeaders.ETag = metadata.ETag;
        responseHeaders.LastModified = metadata.LastModified;
        responseHeaders.Expires = metadata.LastModified.AddSeconds(30);
        response.Headers.Vary = "Accept";
    }
}