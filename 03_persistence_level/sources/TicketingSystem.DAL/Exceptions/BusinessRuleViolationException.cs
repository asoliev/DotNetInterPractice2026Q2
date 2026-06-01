namespace TicketingSystem.DAL.Exceptions;

public class BusinessRuleViolationException(string message) : Exception(message);

public sealed class SoldTicketDeletionNotAllowedException(int eventId)
    : BusinessRuleViolationException($"Event {eventId} cannot be deleted because it has sold tickets.");

public sealed class SeatUnavailableException(int eventSeatId)
    : BusinessRuleViolationException($"Event seat {eventSeatId} is no longer available.");
