# TicketingSystem.AsyncApi

Async REST API for the `04_asynchronous_apis` module.

This project uses the DAL and EF Core infrastructure from the previous module (`03_persistence_level`) and exposes asynchronous endpoints for:

- Venues
- Events
- Orders (cart flow)
- Payments

## Tech Stack

- ASP.NET Core (`net10.0`)
- Entity Framework Core (SQLite)
- Repository + Unit Of Work from previous module

## Project References

The API references these projects from module 03:

- `TicketingSystem.DAL`
- `TicketingSystem.DAL.EF`
- `TicketingSystem.Domain`

## Database

- SQLite file used by API: `03_persistence_level/sources/ticketing.db`
- Connection is configured in `Program.cs`
- On startup, `SeedData.InitializeAsync(...)` ensures database exists and adds demo data if empty.

## Build And Run

From repository root:

```bash
dotnet build 04_asynchronous_apis/sources/TicketingSystem.AsyncApi.slnx
dotnet run --project 04_asynchronous_apis/sources/TicketingSystem.AsyncApi/TicketingSystem.AsyncApi.csproj --urls http://localhost:5188
```

## API Endpoints

Base URL used below: `http://localhost:5188`

### Venues

- `GET /venues`
- `GET /venues/{venue_id}/sections`

### Events

- `GET /events`
- `GET /events/{event_id}/sections/{section_id}/seats`

Seats response includes:

- `sectionId`, `rowId`, `seatId`
- `status` (`id`, `name`)
- `priceOptions` (`id`, `name`, `amount`)

### Orders (Carts)

- `GET /orders/carts/{cart_id}`
- `POST /orders/carts/{cart_id}`
- `DELETE /orders/carts/{cart_id}/events/{event_id}/seats/{seat_id}`
- `PUT /orders/carts/{cart_id}/book`

`POST /orders/carts/{cart_id}` request body:

```json
{
  "eventId": 1,
  "seatId": 2,
  "priceId": 1
}
```

`PUT /orders/carts/{cart_id}/book` returns:

```json
{
  "paymentId": "guid-value"
}
```

### Payments

- `GET /payments/{payment_id}`
- `POST /payments/{payment_id}/complete`
- `POST /payments/{payment_id}/failed`

## Notes

- Cart state is kept in memory (`InMemoryCartStore`) keyed by client-provided cart GUID.
- Payment state is kept in memory (`InMemoryPaymentStore`) keyed by generated payment GUID.
- Booking flow marks seats as `Booked` and creates an order.
- Completing payment marks related seats as `Sold`.
- Failing payment marks related seats back to `Available`.

## Quick Smoke Test Example

```bash
cart_id=$(uuidgen | tr 'A-Z' 'a-z')

curl -s http://localhost:5188/events/1/sections/1/seats

curl -s -X POST "http://localhost:5188/orders/carts/$cart_id" \
  -H "Content-Type: application/json" \
  -d '{"eventId":1,"seatId":2,"priceId":1}'

book_response=$(curl -s -X PUT "http://localhost:5188/orders/carts/$cart_id/book")
echo "$book_response"
```