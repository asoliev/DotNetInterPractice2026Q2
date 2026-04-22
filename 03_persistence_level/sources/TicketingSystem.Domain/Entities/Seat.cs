namespace TicketingSystem.Domain.Entities;

public class Seat
{
    public int Id { get; set; }
    public int SectionId { get; set; }
    public int Row { get; set; }
    public int Number { get; set; }

    public Section Section { get; set; } = null!;
    public ICollection<EventSeat> EventSeats { get; set; } = new List<EventSeat>();
}
