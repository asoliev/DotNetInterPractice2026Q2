namespace TicketingSystem.AsyncApi.Contracts.Responses;

public class VenueSectionResponse
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int SeatsPerRow { get; set; }
}