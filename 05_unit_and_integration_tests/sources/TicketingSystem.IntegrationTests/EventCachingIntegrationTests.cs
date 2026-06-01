using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using TicketingSystem.AsyncApi.Contracts;
using TicketingSystem.AsyncApi.Contracts.Responses;

namespace TicketingSystem.IntegrationTests;

public class EventCachingIntegrationTests(TicketingWebApplicationFactory factory) : IClassFixture<TicketingWebApplicationFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task GetEvents_ReturnsCachingHeaders_And_304ForMatchingEtag()
    {
        HttpResponseMessage firstResponse = await _client.GetAsync("/events");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.True(firstResponse.Headers.ETag is not null);
        Assert.True(firstResponse.Headers.CacheControl?.Public);
        Assert.Equal(TimeSpan.FromSeconds(30), firstResponse.Headers.CacheControl?.MaxAge);

        var conditionalRequest = new HttpRequestMessage(HttpMethod.Get, "/events");
        conditionalRequest.Headers.IfNoneMatch.Add(firstResponse.Headers.ETag);

        HttpResponseMessage secondResponse = await _client.SendAsync(conditionalRequest);

        Assert.Equal(HttpStatusCode.NotModified, secondResponse.StatusCode);
    }

    [Fact]
    public async Task GetEventSeats_ReturnsCachingHeaders_And_304ForMatchingEtag()
    {
        int eventId = await GetFirstEventIdAsync();
        int sectionId = 1;

        HttpResponseMessage firstResponse = await _client.GetAsync($"/events/{eventId}/sections/{sectionId}/seats");

        Assert.Equal(HttpStatusCode.OK, firstResponse.StatusCode);
        Assert.True(firstResponse.Headers.ETag is not null);
        Assert.True(firstResponse.Content.Headers.LastModified is not null);

        var conditionalRequest = new HttpRequestMessage(HttpMethod.Get, $"/events/{eventId}/sections/{sectionId}/seats");
        conditionalRequest.Headers.IfNoneMatch.Add(firstResponse.Headers.ETag);

        HttpResponseMessage secondResponse = await _client.SendAsync(conditionalRequest);

        Assert.Equal(HttpStatusCode.NotModified, secondResponse.StatusCode);
    }

    [Fact]
    public async Task EventCache_IsInvalidatedAfterOrderPostAndPut()
    {
        int eventId = await GetFirstEventIdAsync();
        int seatId = await GetFirstAvailableSeatIdAsync(eventId, sectionId: 1);
        var cartId = Guid.NewGuid();

        string initialEventsEtag = await GetEventsEtagAsync();

        HttpResponseMessage addSeatResponse = await _client.PostAsJsonAsync(
            $"/orders/carts/{cartId}",
            new AddSeatToCartRequest
            {
                EventId = eventId,
                SeatId = seatId,
                PriceId = 1
            });

        Assert.Equal(HttpStatusCode.OK, addSeatResponse.StatusCode);

        string afterPostEventsEtag = await GetEventsEtagAsync();
        Assert.NotEqual(initialEventsEtag, afterPostEventsEtag);

        HttpResponseMessage bookResponse = await _client.PutAsync($"/orders/carts/{cartId}/book", null);

        Assert.Equal(HttpStatusCode.OK, bookResponse.StatusCode);

        string afterPutEventsEtag = await GetEventsEtagAsync();
        Assert.NotEqual(afterPostEventsEtag, afterPutEventsEtag);
    }

    [Fact]
    public async Task EventSeatsCache_IsInvalidatedAfterOrderPostAndPut()
    {
        int eventId = await GetFirstEventIdAsync();
        int sectionId = 1;
        int seatId = await GetFirstAvailableSeatIdAsync(eventId, sectionId);
        var cartId = Guid.NewGuid();

        string initialSeatsEtag = await GetEventSeatsEtagAsync(eventId, sectionId);

        HttpResponseMessage addSeatResponse = await _client.PostAsJsonAsync(
            $"/orders/carts/{cartId}",
            new AddSeatToCartRequest
            {
                EventId = eventId,
                SeatId = seatId,
                PriceId = 1
            });

        Assert.Equal(HttpStatusCode.OK, addSeatResponse.StatusCode);

        string afterPostSeatsEtag = await GetEventSeatsEtagAsync(eventId, sectionId);
        Assert.NotEqual(initialSeatsEtag, afterPostSeatsEtag);

        HttpResponseMessage bookResponse = await _client.PutAsync($"/orders/carts/{cartId}/book", null);

        Assert.Equal(HttpStatusCode.OK, bookResponse.StatusCode);

        string afterPutSeatsEtag = await GetEventSeatsEtagAsync(eventId, sectionId);
        Assert.NotEqual(afterPostSeatsEtag, afterPutSeatsEtag);
    }

    private async Task<int> GetFirstEventIdAsync()
    {
        List<EventResponse>? events = await _client.GetFromJsonAsync<List<EventResponse>>("/events");

        Assert.NotNull(events);
        Assert.NotEmpty(events);

        return events[0].Id;
    }

    private async Task<int> GetFirstAvailableSeatIdAsync(int eventId, int sectionId)
    {
        List<EventSeatResponse>? seats = await _client.GetFromJsonAsync<List<EventSeatResponse>>(
            $"/events/{eventId}/sections/{sectionId}/seats");

        Assert.NotNull(seats);
        Assert.NotEmpty(seats);

        EventSeatResponse availableSeat = seats.First(seat => seat.Status.Id == 0);
        return availableSeat.SeatId;
    }

    private async Task<string> GetEventsEtagAsync()
    {
        HttpResponseMessage response = await _client.GetAsync("/events");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.ETag is not null);

        return response.Headers.ETag!.Tag;
    }

    private async Task<string> GetEventSeatsEtagAsync(int eventId, int sectionId)
    {
        HttpResponseMessage response = await _client.GetAsync($"/events/{eventId}/sections/{sectionId}/seats");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.ETag is not null);

        return response.Headers.ETag!.Tag;
    }
}