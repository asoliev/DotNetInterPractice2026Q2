namespace TicketingSystem.DAL.Exceptions;

public class BusinessRuleViolationException : Exception
{
    public BusinessRuleViolationException(string message) : base(message)
    {
    }
}

public sealed class SoldTicketDeletionNotAllowedException : BusinessRuleViolationException
{
    public SoldTicketDeletionNotAllowedException(int eventId)
        : base($"Event {eventId} cannot be deleted because it has sold tickets.")
    {
    }
}

public sealed class SeatUnavailableException : BusinessRuleViolationException
{
    public SeatUnavailableException(int eventSeatId)
        : base($"Event seat {eventSeatId} is no longer available.")
    {
    }
}
