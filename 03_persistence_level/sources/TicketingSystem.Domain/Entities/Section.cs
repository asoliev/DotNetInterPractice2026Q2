namespace TicketingSystem.Domain.Entities;

public class Section
{
    public int Id { get; set; }
    public int VenueId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int RowCount { get; set; }
    public int SeatsPerRow { get; set; }

    public Venue Venue { get; set; } = null!;
    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
