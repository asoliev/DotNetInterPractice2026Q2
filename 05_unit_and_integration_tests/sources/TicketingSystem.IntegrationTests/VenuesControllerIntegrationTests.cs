using System.Net;
using System.Net.Http.Json;
using TicketingSystem.AsyncApi.Contracts.Responses;

namespace TicketingSystem.IntegrationTests;

public class VenuesControllerIntegrationTests : IClassFixture<TicketingWebApplicationFactory>
{
    private readonly HttpClient _client;

    public VenuesControllerIntegrationTests(TicketingWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetVenues_ReturnsSeededVenueList()
    {
        var response = await _client.GetAsync("/venues");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var venues = await response.Content.ReadFromJsonAsync<List<VenueResponse>>();
        Assert.NotNull(venues);
        Assert.NotEmpty(venues);

        var first = venues[0];
        Assert.NotEqual(0, first.Id);
        Assert.False(string.IsNullOrWhiteSpace(first.Name));
        Assert.True(first.SectionsCount >= 1);
    }

    [Fact]
    public async Task GetVenueSections_ReturnsSections_WhenVenueExists()
    {
        var venues = await _client.GetFromJsonAsync<List<VenueResponse>>("/venues");
        Assert.NotNull(venues);
        Assert.NotEmpty(venues);

        int venueId = venues[0].Id;
        var response = await _client.GetAsync($"/venues/{venueId}/sections");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var sections = await response.Content.ReadFromJsonAsync<List<VenueSectionResponse>>();
        Assert.NotNull(sections);
        Assert.NotEmpty(sections);
        Assert.All(sections, s => Assert.Equal(venueId, s.VenueId));
    }

    [Fact]
    public async Task GetVenueSections_ReturnsNotFound_WhenVenueMissing()
    {
        var response = await _client.GetAsync("/venues/999999/sections");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
