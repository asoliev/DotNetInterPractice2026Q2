namespace TicketingSystem.AsyncApi.Contracts.Responses;

public class EventResponse
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }
}