using TonysTunit.Flights.Models;
using TonysTunit.Flights.Services;
using TonysTunit.Flights.TUnitTests.Support;

namespace TonysTunit.Flights.TUnitTests;

/// <summary>
/// Unit tests for <see cref="BookingService"/>, using a fake clock and a
/// fixed booking-reference generator so assertions stay deterministic.
/// </summary>
public sealed class BookingServiceTests
{
    [Test]
    public async Task Book_Assigns_The_Fake_Reference_And_Stamps_The_Clock()
    {
        await using var harness = ServiceFactory.Create();
        var flight = await Schedule(harness, seats: 10);

        var result = await harness.Service.BookAsync(Request(flight.Id, seats: 2));

        var success = result as BookingResult.Success;
        await Assert.That(success).IsNotNull();
        await Assert.That(success!.Booking.Reference).IsEqualTo("AF-TEST");
        await Assert.That(success.Booking.BookedAt).IsEqualTo(ServiceFactory.NoonOnTheTwelfth);
        await Assert.That(success.Booking.PassengerEmail).IsEqualTo("ada@example.com");
        await Assert.That(success.Booking.Seats).IsEqualTo(2);

        var remaining = await harness.Service.GetFlightAsync(flight.Id);
        await Assert.That(remaining!.SeatsAvailable).IsEqualTo(8);
    }

    [Test]
    public async Task Book_Returns_Not_Found_When_The_Flight_Does_Not_Exist()
    {
        await using var harness = ServiceFactory.Create();

        var result = await harness.Service.BookAsync(Request(Guid.CreateVersion7()));

        await Assert.That(result).IsTypeOf<BookingResult.FlightNotFound>();
    }

    [Test]
    public async Task Book_Returns_Not_Enough_Seats_When_The_Cabin_Is_Full()
    {
        await using var harness = ServiceFactory.Create();
        var flight = await Schedule(harness, seats: 1);

        var result = await harness.Service.BookAsync(Request(flight.Id, seats: 2));

        var failure = result as BookingResult.NotEnoughSeats;
        await Assert.That(failure).IsNotNull();
        await Assert.That(failure!.Available).IsEqualTo(1);
    }

    [Test]
    public async Task Book_Rejects_A_Flight_That_Has_Already_Departed()
    {
        await using var harness = ServiceFactory.Create();
        var flight = await Schedule(harness, departure: harness.Clock.Now.AddHours(-1));

        var result = await harness.Service.BookAsync(Request(flight.Id));

        await Assert.That(result).IsTypeOf<BookingResult.DepartureInThePast>();
    }

    [Test]
    public async Task Cancel_Returns_Seats_To_The_Flight()
    {
        await using var harness = ServiceFactory.Create();
        var flight = await Schedule(harness, seats: 5);
        var booked = (BookingResult.Success)await harness.Service.BookAsync(Request(flight.Id, seats: 2));

        var cancelled = await harness.Service.CancelAsync(booked.Booking.Id);

        await Assert.That(cancelled).IsTrue();
        var remaining = await harness.Service.GetFlightAsync(flight.Id);
        await Assert.That(remaining!.SeatsAvailable).IsEqualTo(5);
        await Assert.That(await harness.Service.GetBookingAsync(booked.Booking.Id)).IsNull();
    }

    private static Task<Flight> Schedule(
        BookingHarness harness,
        int seats = 20,
        DateTimeOffset? departure = null,
        string number = "AF900") =>
        harness.Service.ScheduleAsync(new CreateFlightRequest
        {
            FlightNumber = number,
            Origin = "LHR",
            Destination = "JFK",
            Departure = departure ?? harness.Clock.Now.AddDays(3),
            Seats = seats
        });

    private static CreateBookingRequest Request(Guid flightId, int seats = 1) =>
        new()
        {
            FlightId = flightId,
            PassengerName = "Ada Lovelace",
            PassengerEmail = "Ada@Example.com",
            Seats = seats
        };
}
