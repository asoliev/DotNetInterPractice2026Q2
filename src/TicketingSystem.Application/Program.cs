using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TicketingSystem.DAL.Exceptions;
using TicketingSystem.DAL.EF;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

try
{
	await RunDemoAsync();
}
catch (Exception ex)
{
	HandleException(ex);
	Environment.ExitCode = 1;
}

static async Task RunDemoAsync()
{
	string logsDirectory = Path.Combine(AppContext.BaseDirectory, "logs");
	Directory.CreateDirectory(logsDirectory);
	string logPath = Path.Combine(logsDirectory, $"ef-queries-{DateTime.UtcNow:yyyyMMdd-HHmmss}.log");
	await File.WriteAllTextAsync(logPath, string.Empty);
    Action<string> queryLogger = message =>
	{
		string line = $"{DateTime.UtcNow:O} {message}";
		File.AppendAllText(logPath, line + Environment.NewLine);
	};

	string dbPath = Path.GetFullPath(Path.Combine(
		AppContext.BaseDirectory,
		"..",
		"..",
		"..",
		"..",
		"ticketing.db"));

	DbContextOptions<TicketingDbContext> options = new DbContextOptionsBuilder<TicketingDbContext>()
		.UseSqlite($"Data Source={dbPath}")
		.LogTo(queryLogger, LogLevel.Information)
		.EnableDetailedErrors()
		.Options;

	await using var context = new TicketingDbContext(options);
	await context.Database.EnsureDeletedAsync();
	await context.Database.EnsureCreatedAsync();

	IUnitOfWork uow = new UnitOfWork(context);

	// --- Seed data ---
	Console.WriteLine("=== Seeding data ===");

	var venue = new Venue { Name = "Grand Arena", Address = "123 Main St" };
	var section = new Section { Name = "VIP", RowCount = 5, SeatsPerRow = 10, Venue = venue };
	var seat1 = new Seat { Row = 1, Number = 1, Section = section };
	var seat2 = new Seat { Row = 1, Number = 2, Section = section };

	var evt = new Event
	{
		Title = "Rock Concert 2026",
		Description = "Best rock event of the year",
		Date = DateTime.UtcNow.AddDays(30),
		Venue = venue
	};

	var customer = new Customer { Name = "Alice Smith", Email = "alice@example.com" };

	await uow.Customers.AddAsync(customer);
	await context.Set<Venue>().AddAsync(venue);
	await context.Set<Section>().AddAsync(section);
	await context.Set<Seat>().AddRangeAsync(seat1, seat2);
	await uow.Events.AddAsync(evt);
	await uow.SaveChangesAsync();

	var es1 = new EventSeat { EventId = evt.Id, SeatId = seat1.Id, Price = 50m, Status = SeatStatus.Available };
	var es2 = new EventSeat { EventId = evt.Id, SeatId = seat2.Id, Price = 75m, Status = SeatStatus.Available };
	await context.Set<EventSeat>().AddRangeAsync(es1, es2);
	await uow.SaveChangesAsync();

	Console.WriteLine($"Created event '{evt.Title}' with {2} seats.");

	Console.WriteLine("\n=== READ: All upcoming events ===");
	IEnumerable<Event> upcoming = await uow.Events.GetUpcomingAsync();
	foreach (Event e in upcoming)
		Console.WriteLine($"  [{e.Id}] {e.Title} at {e.Venue.Name} on {e.Date:yyyy-MM-dd}");

	Console.WriteLine("\n=== READ: Available seats for event ===");
	IEnumerable<EventSeat> available = await uow.EventSeats.GetAvailableByEventIdAsync(evt.Id);
	foreach (EventSeat s in available)
		Console.WriteLine($"  EventSeat [{s.Id}] Row {s.Seat.Row}, Seat {s.Seat.Number} - ${s.Price}");

	Console.WriteLine("\n=== READ: Cheapest available seat ===");
	EventSeat? cheapest = await uow.EventSeats.GetCheapestAvailableAsync(evt.Id);
	Console.WriteLine(cheapest is not null
		? $"  Cheapest: EventSeat [{cheapest.Id}] at ${cheapest.Price}"
		: "  No seats available.");

	Console.WriteLine("\n=== CREATE: New order (buy cheapest seat) ===");
	await uow.BeginTransactionAsync();
	try
	{
		bool changed = await uow.EventSeats.TryChangeStatusAsync(cheapest!.Id, SeatStatus.Available, SeatStatus.Booked);
		if (!changed)
			throw new SeatUnavailableException(cheapest.Id);

		var order = new Order
		{
			CustomerId = customer.Id,
			CreatedAt = DateTime.UtcNow,
			Status = OrderStatus.Confirmed,
			Items = new List<OrderItem>
			{
				new OrderItem { EventSeatId = cheapest.Id, PriceAtPurchase = cheapest.Price }
			}
		};
		await uow.Orders.AddAsync(order);
		await uow.SaveChangesAsync();

		await uow.EventSeats.TryChangeStatusAsync(cheapest.Id, SeatStatus.Booked, SeatStatus.Sold);
		await uow.SaveChangesAsync();
		await uow.CommitTransactionAsync();

		Console.WriteLine($"  Order [{order.Id}] confirmed for customer '{customer.Name}'.");
	}
	catch
	{
		try
		{
			await uow.RollbackTransactionAsync();
		}
		catch
		{
			// Ignore rollback errors to preserve the original exception.
		}

		throw;
	}

	Console.WriteLine("\n=== READ: Customer orders ===");
	IEnumerable<Order> orders = await uow.Orders.GetByCustomerIdAsync(customer.Id);
	foreach (Order o in orders)
		Console.WriteLine($"  Order [{o.Id}] Status={o.Status}, Items={o.Items.Count}");

	Console.WriteLine("\n=== UPDATE: Rename event ===");
	Event? eventToUpdate = await uow.Events.GetByIdAsync(evt.Id);
	eventToUpdate!.Title = "Rock Concert 2026 - UPDATED";
	uow.Events.Update(eventToUpdate);
	await uow.SaveChangesAsync();
	Console.WriteLine($"  Updated title: '{eventToUpdate.Title}'");

	Console.WriteLine("\n=== DELETE: Remove event (validation demo) ===");
	try
	{
		await uow.Events.DeleteEventAsync(evt.Id);
		await uow.SaveChangesAsync();
		Console.WriteLine("  Event deleted.");
	}
	catch (SoldTicketDeletionNotAllowedException ex)
	{
		Console.WriteLine($"  Delete skipped by business rule: {ex.Message}");
	}

	Console.WriteLine("\n=== Done ===");
	Console.WriteLine($"EF query log file: {logPath}");
}

static void HandleException(Exception ex)
{
	switch (ex)
	{
		case BusinessRuleViolationException bre:
			Console.Error.WriteLine($"Business rule violation: {bre.Message}");
			break;
		case DbUpdateConcurrencyException dce:
			Console.Error.WriteLine($"Concurrency error: {dce.Message}");
			break;
		case DbUpdateException due:
			Console.Error.WriteLine($"Database update error: {due.Message}");
			break;
		case KeyNotFoundException knf:
			Console.Error.WriteLine($"Not found: {knf.Message}");
			break;
		default:
			Console.Error.WriteLine($"Unexpected error: {ex.Message}");
			break;
	}
}
