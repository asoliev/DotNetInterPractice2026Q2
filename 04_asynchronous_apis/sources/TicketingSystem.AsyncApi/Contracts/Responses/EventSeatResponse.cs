namespace TicketingSystem.AsyncApi.Contracts.Responses;

public class EventSeatResponse
{
    public int SectionId { get; set; }
    public int RowId { get; set; }
    public int SeatId { get; set; }
    public SeatStatusResponse Status { get; set; } = new();
    public IReadOnlyCollection<PriceOptionResponse> PriceOptions { get; set; } = [];
}

public class SeatStatusResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class PriceOptionResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
}