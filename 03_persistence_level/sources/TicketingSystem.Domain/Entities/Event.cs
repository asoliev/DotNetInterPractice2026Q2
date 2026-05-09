namespace TicketingSystem.Domain.Entities;

public class Event
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime Date { get; set; }

    public Venue Venue { get; set; } = null!;
    public ICollection<EventSeat> EventSeats { get; set; } = new List<EventSeat>();
}
