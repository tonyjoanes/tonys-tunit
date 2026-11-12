using Microsoft.EntityFrameworkCore;
using TonysTunit.Flights.Data;
using TonysTunit.Flights.Services;

namespace TonysTunit.Flights.TUnitTests.Support;

internal sealed class FixedReferences(string value) : IBookingReferenceGenerator
{
    public string Next() => value;
}

internal sealed class FakeClock(DateTimeOffset now) : TimeProvider
{
    public DateTimeOffset Now { get; set; } = now;

    public override DateTimeOffset GetUtcNow() => Now;
}

internal static class ServiceFactory
{
    public static DateTimeOffset NoonOnTheTwelfth { get; } =
        new(2026, 11, 12, 12, 0, 0, TimeSpan.Zero);

    public static BookingHarness Create(string reference = "AF-TEST")
    {
        var options = new DbContextOptionsBuilder<FlightsDbContext>()
            .UseInMemoryDatabase($"unit-{Guid.NewGuid():N}")
            .Options;
        var db = new FlightsDbContext(options);
        var clock = new FakeClock(NoonOnTheTwelfth);
        var refs = new FixedReferences(reference);
        var service = new BookingService(db, refs, clock);
        return new BookingHarness(db, service, clock, refs);
    }
}

internal sealed class BookingHarness(
    FlightsDbContext db,
    BookingService service,
    FakeClock clock,
    FixedReferences references) : IAsyncDisposable
{
    public FlightsDbContext Db { get; } = db;
    public BookingService Service { get; } = service;
    public FakeClock Clock { get; } = clock;
    public FixedReferences References { get; } = references;

    public ValueTask DisposeAsync() => Db.DisposeAsync();
}
