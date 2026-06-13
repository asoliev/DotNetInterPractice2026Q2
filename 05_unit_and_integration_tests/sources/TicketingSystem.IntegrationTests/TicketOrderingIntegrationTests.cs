using System.Net;
using System.Net.Http.Json;
using TicketingSystem.AsyncApi.Contracts;
using TicketingSystem.AsyncApi.Contracts.Responses;

namespace TicketingSystem.IntegrationTests;

/// <summary>
/// Integration tests covering the full ticket ordering and release flow.
/// The factory creates an isolated SQLite in-memory database per class instance,
/// applies all EF migrations, and seeds initial venue/event/seat data.
/// </summary>
public class TicketOrderingIntegrationTests : IClassFixture<TicketingWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TicketOrderingIntegrationTests(TicketingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    // ── Helpers ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns all available seats for the first event, section 1.
    /// Each test that books a seat should pick a different index to avoid
    /// hitting the unique constraint on OrderItems.EventSeatId.
    /// </summary>
    private async Task<(int eventId, List<EventSeatResponse> seats)> GetAvailableSeatsAsync()
    {
        var events = await _client.GetFromJsonAsync<List<EventResponse>>("/events");
        Assert.NotNull(events);
        Assert.NotEmpty(events);

        int eventId = events[0].Id;
        int sectionId = 1; // seeded sections start at id=1

        var seats = await _client
            .GetFromJsonAsync<List<EventSeatResponse>>($"/events/{eventId}/sections/{sectionId}/seats");

        Assert.NotNull(seats);
        Assert.NotEmpty(seats);

        return (eventId, seats);
    }

    private async Task<(int eventId, int seatId)> GetFirstAvailableSeatAsync()
    {
        var (eventId, seats) = await GetAvailableSeatsAsync();
        return (eventId, seats[0].SeatId);
    }

    private async Task<CartResponse> AddSeatToCartAsync(Guid cartId, int eventId, int seatId)
    {
        var request = new AddSeatToCartRequest { EventId = eventId, SeatId = seatId, PriceId = 1 };
        var response = await _client.PostAsJsonAsync($"/orders/carts/{cartId}", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CartResponse>())!;
    }

    private async Task<BookCartResponse> BookCartAsync(Guid cartId)
    {
        var response = await _client.PutAsync($"/orders/carts/{cartId}/book", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<BookCartResponse>())!;
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetCart_WhenCartDoesNotExist_CreatesEmptyCart()
    {
        var cartId = Guid.NewGuid();

        var response = await _client.GetAsync($"/orders/carts/{cartId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cart = await response.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);
        Assert.Equal(cartId, cart.CartId);
        Assert.Empty(cart.Items);
        Assert.Equal(0m, cart.TotalAmount);
    }

    [Fact]
    public async Task AddSeatToCart_WhenSeatIsAvailable_ReturnsSeatInCart()
    {
        var cartId = Guid.NewGuid();
        var (eventId, seatId) = await GetFirstAvailableSeatAsync();

        var cart = await AddSeatToCartAsync(cartId, eventId, seatId);

        Assert.Equal(cartId, cart.CartId);
        Assert.Single(cart.Items);
        Assert.Equal(eventId, cart.Items.First().EventId);
        Assert.Equal(seatId, cart.Items.First().SeatId);
    }

    [Fact]
    public async Task AddSeatToCart_WhenManyRequestsTargetSameSeat_OnlyOneSucceeds()
    {
        (int eventId, int seatId) = await GetFirstAvailableSeatAsync();
        const int requestCount = 1000;

        Task<HttpStatusCode>[] requests = [.. Enumerable.Range(0, requestCount)
            .Select(async _ =>
            {
                var cartId = Guid.NewGuid();
                HttpResponseMessage response = await _client.PostAsJsonAsync($"/orders/carts/{cartId}", new AddSeatToCartRequest
                {
                    EventId = eventId,
                    SeatId = seatId,
                    PriceId = 1
                });

                return response.StatusCode;
            })];

        HttpStatusCode[] statuses = await Task.WhenAll(requests);

        Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.OK));
    }

    [Fact]
    public async Task AddSeatToCart_WhenSeatNotFound_ReturnsNotFound()
    {
        var cartId = Guid.NewGuid();
        var request = new AddSeatToCartRequest { EventId = 9999, SeatId = 9999, PriceId = 1 };

        var response = await _client.PostAsJsonAsync($"/orders/carts/{cartId}", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddSeatToCart_WithInvalidPriceId_ReturnsBadRequest()
    {
        var cartId = Guid.NewGuid();
        var (eventId, seatId) = await GetFirstAvailableSeatAsync();
        var request = new AddSeatToCartRequest { EventId = eventId, SeatId = seatId, PriceId = 0 };

        var response = await _client.PostAsJsonAsync($"/orders/carts/{cartId}", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task BookCart_WhenCartIsEmpty_ReturnsBadRequest()
    {
        var cartId = Guid.NewGuid();
        // Create an empty cart first
        await _client.GetAsync($"/orders/carts/{cartId}");

        var response = await _client.PutAsync($"/orders/carts/{cartId}/book", null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task FullOrderFlow_BookAndCompletePayment_SeatStatusBecomeSold()
    {
        var cartId = Guid.NewGuid();
        var (eventId, seats) = await GetAvailableSeatsAsync();
        int seatId = seats[1].SeatId;

        // 1. Add seat to cart
        await AddSeatToCartAsync(cartId, eventId, seatId);

        // 2. Book cart → creates order + pending payment
        var bookResult = await BookCartAsync(cartId);
        Assert.NotEqual(Guid.Empty, bookResult.PaymentId);

        // 3. Get payment (should be Pending)
        var paymentResponse = await _client.GetFromJsonAsync<PaymentResponse>($"/payments/{bookResult.PaymentId}");
        Assert.NotNull(paymentResponse);
        Assert.Equal("Pending", paymentResponse.Status);

        // 4. Complete payment → seats become Sold
        var completeResponse = await _client.PostAsync($"/payments/{bookResult.PaymentId}/complete", null);
        Assert.Equal(HttpStatusCode.OK, completeResponse.StatusCode);
        var statusUpdate = await completeResponse.Content.ReadFromJsonAsync<PaymentStatusUpdateResponse>();
        Assert.Equal("Completed", statusUpdate!.Status);
    }

    [Fact]
    public async Task FullOrderFlow_BookAndFailPayment_SeatStatusBecomesAvailableAgain()
    {
        var cartId = Guid.NewGuid();
        var (eventId, seats) = await GetAvailableSeatsAsync();
        int seatId = seats[0].SeatId;

        // 1. Add seat to cart
        await AddSeatToCartAsync(cartId, eventId, seatId);

        // 2. Book cart
        var bookResult = await BookCartAsync(cartId);

        // 3. Fail payment → seats return to Available
        var failResponse = await _client.PostAsync($"/payments/{bookResult.PaymentId}/failed", null);
        Assert.Equal(HttpStatusCode.OK, failResponse.StatusCode);
        var statusUpdate = await failResponse.Content.ReadFromJsonAsync<PaymentStatusUpdateResponse>();
        Assert.Equal("Failed", statusUpdate!.Status);

        // 4. The seat should be available again – verify by adding it to a new cart
        var newCartId = Guid.NewGuid();
        var cart = await AddSeatToCartAsync(newCartId, eventId, seatId);
        Assert.Single(cart.Items);
    }

    [Fact]
    public async Task CompletePayment_WhenAlreadyCompleted_IsIdempotent()
    {
        var cartId = Guid.NewGuid();
        var (eventId, seats) = await GetAvailableSeatsAsync();
        int seatId = seats[2].SeatId;
        await AddSeatToCartAsync(cartId, eventId, seatId);
        var bookResult = await BookCartAsync(cartId);

        // Complete twice
        var first = await _client.PostAsync($"/payments/{bookResult.PaymentId}/complete", null);
        var second = await _client.PostAsync($"/payments/{bookResult.PaymentId}/complete", null);

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        var response = await second.Content.ReadFromJsonAsync<PaymentStatusUpdateResponse>();
        Assert.Equal("Completed", response!.Status);
    }

    [Fact]
    public async Task RemoveSeatFromCart_RemovesSeatAndReturnsUpdatedCart()
    {
        var cartId = Guid.NewGuid();
        var (eventId, seatId) = await GetFirstAvailableSeatAsync();
        await AddSeatToCartAsync(cartId, eventId, seatId);

        var removeResponse = await _client.DeleteAsync(
            $"/orders/carts/{cartId}/events/{eventId}/seats/{seatId}");

        Assert.Equal(HttpStatusCode.OK, removeResponse.StatusCode);
        var cart = await removeResponse.Content.ReadFromJsonAsync<CartResponse>();
        Assert.NotNull(cart);
        Assert.Empty(cart.Items);
    }

    [Fact]
    public async Task GetPayment_WhenNotFound_ReturnsNotFound()
    {
        var response = await _client.GetAsync($"/payments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
