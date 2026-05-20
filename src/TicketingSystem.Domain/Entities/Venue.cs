namespace TicketingSystem.Domain.Entities;

public class Venue
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;

    public ICollection<Section> Sections { get; set; } = new List<Section>();
    public ICollection<Event> Events { get; set; } = new List<Event>();
}
