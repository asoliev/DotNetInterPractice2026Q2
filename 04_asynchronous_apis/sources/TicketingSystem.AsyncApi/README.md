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

- SQLite file used by API: `04_asynchronous_apis/sources/ticketing.asyncapi.db`
- Connection is configured in `Program.cs`
- On startup, `SeedData.InitializeAsync(...)` applies EF migrations and then seeds demo data if empty.

## Build And Run

From repository root:

```bash
dotnet build 04_asynchronous_apis/sources/TicketingSystem.AsyncApi.slnx
dotnet run --project 04_asynchronous_apis/sources/TicketingSystem.AsyncApi/TicketingSystem.AsyncApi.csproj --urls http://localhost:5188
```

## Swagger UI

After starting the app, open:

- `http://localhost:5188/swagger`

Swagger UI lets you:

- browse all controllers and routes,
- execute requests manually,
- inspect request/response schemas,
- copy curl commands for terminal testing.

Recommended manual flow in Swagger:

1. Call `GET /events` and note an `eventId`.
2. Call `GET /events/{event_id}/sections/{section_id}/seats` and choose an available `seatId`.
3. Generate a cart GUID (for example with `uuidgen`) and call `POST /orders/carts/{cart_id}`.
4. Call `GET /orders/carts/{cart_id}` to verify cart content and total amount.
5. Call `PUT /orders/carts/{cart_id}/book` and copy returned `paymentId`.
6. Call `GET /payments/{payment_id}`.
7. Call either `POST /payments/{payment_id}/complete` or `POST /payments/{payment_id}/failed`.

## Health Endpoints

The API exposes lightweight health endpoints:

- `GET /health`
- `GET /health/live`
- `GET /health/ready`

All three are useful for local checks and deployment probes.

## Startup Behavior

- Root path `/` redirects to `/swagger`.
- Launch profile is configured to open Swagger automatically (`launchUrl: swagger`).

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

- Cart state is persisted in SQLite (`Carts` and `CartItems` tables) keyed by client-provided cart GUID.
- Payment state is persisted in SQLite (`Payments` table) keyed by generated payment GUID.
- Booking flow marks seats as `Booked`, creates an order, and creates a pending payment.
- Completing payment marks related seats as `Sold`.
- Failing payment marks related seats back to `Available`.
- After successful add-to-cart and checkout operations, the API enqueues notification messages into an in-memory queue and a hosted background service logs the email-provider flow.

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
