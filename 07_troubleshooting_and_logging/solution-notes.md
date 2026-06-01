# Troubleshooting and Logging Module Solutions

## Task 1: Pessimistic Concurrency
The POST `orders/carts/{cart_id}` endpoint was first reworked to use a pessimistic approach. It was added an in-process booking gate so requests for the same seat are handled one at a time inside the app instance. This prevents two requests from booking the same seat at the same moment.

## Task 2: Optimistic Concurrency
For the final solution, It was switched the booking flow to optimistic concurrency.

What changed:
- `EventSeat.Status` was marked as a concurrency token.
- The POST endpoint now updates the seat status to `Booked` and saves it.
- If another request changes the same seat first, EF Core throws a concurrency exception.
- The controller returns `409 Conflict` when the seat is no longer available.

Why this works:
- The database now detects the conflict instead of the app relying on a local lock.
- Only one request can win the update for the same seat.
- Parallel booking tests with 1000 requests should result in only one successful response.

## Short Summary
- Task 1: prevent overlapping bookings with a local lock.
- Task 2: let EF Core detect and reject conflicting seat updates using optimistic concurrency.
