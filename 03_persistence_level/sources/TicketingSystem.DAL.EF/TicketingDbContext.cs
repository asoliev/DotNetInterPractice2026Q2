using Microsoft.EntityFrameworkCore;
using TicketingSystem.Domain.Entities;
using TicketingSystem.Domain.Enums;

namespace TicketingSystem.DAL.EF;

public class TicketingDbContext(DbContextOptions<TicketingDbContext> options) : DbContext(options)
{
    public DbSet<Venue> Venues => Set<Venue>();
    public DbSet<Section> Sections => Set<Section>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventSeat> EventSeats => Set<EventSeat>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Venue
        modelBuilder.Entity<Venue>(e =>
        {
            e.HasKey(v => v.Id);
            e.Property(v => v.Name).IsRequired().HasMaxLength(200);
            e.Property(v => v.Address).IsRequired().HasMaxLength(500);
        });

        // Section
        modelBuilder.Entity<Section>(e =>
        {
            e.HasKey(s => s.Id);
            e.Property(s => s.Name).IsRequired().HasMaxLength(100);
            e.HasOne(s => s.Venue)
                .WithMany(v => v.Sections)
                .HasForeignKey(s => s.VenueId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Seat
        modelBuilder.Entity<Seat>(e =>
        {
            e.HasKey(s => s.Id);
            e.HasOne(s => s.Section)
                .WithMany(sec => sec.Seats)
                .HasForeignKey(s => s.SectionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Event
        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.Property(ev => ev.Title).IsRequired().HasMaxLength(200);
            e.Property(ev => ev.Description).HasMaxLength(1000);
            e.HasOne(ev => ev.Venue)
                .WithMany(v => v.Events)
                .HasForeignKey(ev => ev.VenueId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // EventSeat
        modelBuilder.Entity<EventSeat>(e =>
        {
            e.HasKey(es => es.Id);
            e.Property(es => es.Price).HasColumnType("decimal(18,2)");
            e.Property(es => es.Status).HasConversion<int>();
            e.HasIndex(es => new { es.EventId, es.SeatId }).IsUnique();
            e.HasOne(es => es.Event)
                .WithMany(ev => ev.EventSeats)
                .HasForeignKey(es => es.EventId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(es => es.Seat)
                .WithMany(s => s.EventSeats)
                .HasForeignKey(es => es.SeatId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Customer
        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(c => c.Id);
            e.Property(c => c.Name).IsRequired().HasMaxLength(200);
            e.Property(c => c.Email).IsRequired().HasMaxLength(200);
            e.HasIndex(c => c.Email).IsUnique();
        });

        // Order
        modelBuilder.Entity<Order>(e =>
        {
            e.HasKey(o => o.Id);
            e.Property(o => o.Status).HasConversion<int>();
            e.HasOne(o => o.Customer)
                .WithMany(c => c.Orders)
                .HasForeignKey(o => o.CustomerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // OrderItem
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.HasKey(oi => oi.Id);
            e.Property(oi => oi.PriceAtPurchase).HasColumnType("decimal(18,2)");
            e.HasOne(oi => oi.Order)
                .WithMany(o => o.Items)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);
            e.HasOne(oi => oi.EventSeat)
                .WithOne(es => es.OrderItem)
                .HasForeignKey<OrderItem>(oi => oi.EventSeatId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
