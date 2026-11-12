using Microsoft.EntityFrameworkCore;
using TonysTunit.Flights.Models;

namespace TonysTunit.Flights.Data;

public sealed class FlightsDbContext(DbContextOptions<FlightsDbContext> options) : DbContext(options)
{
    public DbSet<Flight> Flights => Set<Flight>();
    public DbSet<Booking> Bookings => Set<Booking>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Flight>(entity =>
        {
            entity.HasKey(f => f.Id);
            entity.Property(f => f.FlightNumber).HasMaxLength(8);
            entity.Property(f => f.Origin).HasMaxLength(3);
            entity.Property(f => f.Destination).HasMaxLength(3);
        });

        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.Property(b => b.Reference).HasMaxLength(16);
            entity.HasIndex(b => b.Reference).IsUnique();
            entity.HasOne(b => b.Flight)
                .WithMany()
                .HasForeignKey(b => b.FlightId);
        });
    }
}
