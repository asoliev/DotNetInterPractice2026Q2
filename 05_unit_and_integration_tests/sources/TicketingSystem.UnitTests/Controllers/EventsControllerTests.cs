using Microsoft.AspNetCore.Mvc;
using Moq;
using TicketingSystem.AsyncApi.Contracts.Responses;
using TicketingSystem.AsyncApi.Controllers;
using TicketingSystem.DAL.Interfaces;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.UnitTests.Controllers;

public class EventsControllerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly Mock<IEventRepository> _eventRepoMock = new();
    private readonly Mock<IEventSeatRepository> _eventSeatRepoMock = new();

    public EventsControllerTests()
    {
        _unitOfWorkMock.Setup(u => u.Events).Returns(_eventRepoMock.Object);
        _unitOfWorkMock.Setup(u => u.EventSeats).Returns(_eventSeatRepoMock.Object);
    }

    [Fact]
    public async Task GetEventsAsync_ReturnsOkWithEventsSortedByDate()
    {
        // Arrange
        var events = new List<Event>
        {
            new() { Id = 1, VenueId = 1, Title = "B Event", Date = DateTime.UtcNow.AddDays(5) },
            new() { Id = 2, VenueId = 1, Title = "A Event", Date = DateTime.UtcNow.AddDays(1) }
        };
        _eventRepoMock.Setup(r => r.GetAllAsync()).ReturnsAsync(events);

        var controller = new EventsController(_unitOfWorkMock.Object);

        // Act
        var result = await controller.GetEventsAsync();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IReadOnlyCollection<EventResponse>>(ok.Value);
        Assert.Equal(2, response.Count);
        Assert.Equal("A Event", response.First().Title); // sorted by date ascending
    }

    [Fact]
    public async Task GetSectionSeatsAsync_ReturnsNotFound_WhenEventDoesNotExist()
    {
        // Arrange
        _eventRepoMock.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Event?)null);
        var controller = new EventsController(_unitOfWorkMock.Object);

        // Act
        var result = await controller.GetSectionSeatsAsync(99, 1);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetSectionSeatsAsync_ReturnsOk_WithSeatsFilteredBySection()
    {
        // Arrange
        int eventId = 1;
        int sectionId = 10;

        var section = new Domain.Entities.Section { Id = sectionId, Name = "A" };
        var seat1 = new Seat { Id = 1, SectionId = sectionId, Row = 1, Section = section };
        var seat2 = new Seat { Id = 2, SectionId = 99, Row = 1, Section = new Domain.Entities.Section { Id = 99, Name = "B" } };

        var eventSeats = new List<EventSeat>
        {
            new() { Id = 1, EventId = eventId, SeatId = 1, Price = 50m, Status = SeatStatus.Available, Seat = seat1 },
            new() { Id = 2, EventId = eventId, SeatId = 2, Price = 60m, Status = SeatStatus.Available, Seat = seat2 }
        };

        _eventRepoMock.Setup(r => r.GetByIdAsync(eventId)).ReturnsAsync(new Event { Id = eventId });
        _eventSeatRepoMock.Setup(r => r.GetByEventIdAsync(eventId)).ReturnsAsync(eventSeats);

        var controller = new EventsController(_unitOfWorkMock.Object);

        // Act
        var result = await controller.GetSectionSeatsAsync(eventId, sectionId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsAssignableFrom<IReadOnlyCollection<EventSeatResponse>>(ok.Value);
        Assert.Single(response); // only seat1 belongs to sectionId
    }
}
