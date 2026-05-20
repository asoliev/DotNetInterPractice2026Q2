using Microsoft.EntityFrameworkCore;
using TicketingSystem.DAL.EF;
using TicketingSystem.DAL.EF.Repositories;
using TicketingSystem.Domain.Entities;

namespace TicketingSystem.UnitTests.Repositories;

public class OrderRepositoryTests : IDisposable
{
    private readonly TicketingDbContext _context;
    private readonly OrderRepository _repository;

    public OrderRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<TicketingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new TicketingDbContext(options);
        _repository = new OrderRepository(_context);
    }

    public void Dispose() => _context.Dispose();

    [Fact]
    public async Task GetWithItemsAsync_ReturnsOrderGraph_WhenExists()
    {
        var customer = new Customer { Name = "C", Email = "c@x.com" };
        await _context.Customers.AddAsync(customer);
        await _context.SaveChangesAsync();

        var order = new Order { CustomerId = customer.Id, CreatedAt = DateTime.UtcNow };
        await _context.Orders.AddAsync(order);
        await _context.SaveChangesAsync();

        var venue = new Venue { Name = "V", Address = "A" };
        var section = new Section { Venue = venue, Name = "S", RowCount = 1, SeatsPerRow = 1 };
        var seat = new Seat { Section = section, Row = 1, Number = 1 };
        var ev = new Event { Venue = venue, Title = "E", Date = DateTime.UtcNow.AddDays(1) };
        var eventSeat = new EventSeat { Event = ev, Seat = seat, Price = 50m };
        await _context.AddRangeAsync(venue, section, seat, ev, eventSeat);
        await _context.SaveChangesAsync();

        await _context.OrderItems.AddAsync(new OrderItem { OrderId = order.Id, EventSeatId = eventSeat.Id, PriceAtPurchase = 50m });
        await _context.SaveChangesAsync();

        var result = await _repository.GetWithItemsAsync(order.Id);

        Assert.NotNull(result);
        Assert.NotNull(result!.Customer);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetWithItemsAsync_ReturnsNull_WhenMissing()
    {
        var result = await _repository.GetWithItemsAsync(999);
        Assert.Null(result);
    }

    [Fact]
    public async Task GetByCustomerIdAsync_ReturnsOnlyCustomerOrders_OrderedByCreatedAtDesc()
    {
        var customer1 = new Customer { Name = "C1", Email = "c1@x.com" };
        var customer2 = new Customer { Name = "C2", Email = "c2@x.com" };
        await _context.Customers.AddRangeAsync(customer1, customer2);
        await _context.SaveChangesAsync();

        var oldOrder = new Order { CustomerId = customer1.Id, CreatedAt = DateTime.UtcNow.AddHours(-2) };
        var newOrder = new Order { CustomerId = customer1.Id, CreatedAt = DateTime.UtcNow.AddHours(-1) };
        var otherOrder = new Order { CustomerId = customer2.Id, CreatedAt = DateTime.UtcNow };

        await _context.Orders.AddRangeAsync(oldOrder, newOrder, otherOrder);
        await _context.SaveChangesAsync();

        var result = (await _repository.GetByCustomerIdAsync(customer1.Id)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(newOrder.Id, result[0].Id);
        Assert.Equal(oldOrder.Id, result[1].Id);
        Assert.All(result, o => Assert.Equal(customer1.Id, o.CustomerId));
    }
}
