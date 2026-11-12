// Matches TUnit's BookingServiceTests.cs. The same unit tests, written
// with xUnit's Assert.Equal and friends rather than await Assert.That.

using TonysTunit.Flights.Models;
using TonysTunit.Flights.XunitTests.Support;

namespace TonysTunit.Flights.XunitTests;

public sealed class BookingServiceTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Book_Assigns_The_Fake_Reference_And_Stamps_The_Clock()
    {
        await using var harness = ServiceFactory.Create();
        var flight = await Schedule(harness, seats: 10);

        var result = await harness.Service.BookAsync(Request(flight.Id, seats: 2), Ct);

        var success = Assert.IsType<BookingResult.Success>(result);
        Assert.Equal("AF-TEST", success.Booking.Reference);
        Assert.Equal(ServiceFactory.NoonOnTheTwelfth, success.Booking.BookedAt);
        Assert.Equal("ada@example.com", success.Booking.PassengerEmail);
        Assert.Equal(2, success.Booking.Seats);

        var remaining = await harness.Service.GetFlightAsync(flight.Id, Ct);
        Assert.Equal(8, remaining!.SeatsAvailable);
    }

    [Fact]
    public async Task Book_Returns_Not_Found_When_The_Flight_Does_Not_Exist()
    {
        await using var harness = ServiceFactory.Create();

        var result = await harness.Service.BookAsync(Request(Guid.CreateVersion7()), Ct);

        Assert.IsType<BookingResult.FlightNotFound>(result);
    }

    [Fact]
    public async Task Book_Returns_Not_Enough_Seats_When_The_Cabin_Is_Full()
    {
        await using var harness = ServiceFactory.Create();
        var flight = await Schedule(harness, seats: 1);

        var result = await harness.Service.BookAsync(Request(flight.Id, seats: 2), Ct);

        var failure = Assert.IsType<BookingResult.NotEnoughSeats>(result);
        Assert.Equal(1, failure.Available);
    }

    [Fact]
    public async Task Book_Rejects_A_Flight_That_Has_Already_Departed()
    {
        await using var harness = ServiceFactory.Create();
        var flight = await Schedule(harness, departure: harness.Clock.Now.AddHours(-1));

        var result = await harness.Service.BookAsync(Request(flight.Id), Ct);

        Assert.IsType<BookingResult.DepartureInThePast>(result);
    }

    [Fact]
    public async Task Cancel_Returns_Seats_To_The_Flight()
    {
        await using var harness = ServiceFactory.Create();
        var flight = await Schedule(harness, seats: 5);
        var booked = Assert.IsType<BookingResult.Success>(
            await harness.Service.BookAsync(Request(flight.Id, seats: 2), Ct));

        var cancelled = await harness.Service.CancelAsync(booked.Booking.Id, Ct);

        Assert.True(cancelled);
        var remaining = await harness.Service.GetFlightAsync(flight.Id, Ct);
        Assert.Equal(5, remaining!.SeatsAvailable);
        Assert.Null(await harness.Service.GetBookingAsync(booked.Booking.Id, Ct));
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
        }, Ct);

    private static CreateBookingRequest Request(Guid flightId, int seats = 1) =>
        new()
        {
            FlightId = flightId,
            PassengerName = "Ada Lovelace",
            PassengerEmail = "Ada@Example.com",
            Seats = seats
        };
}
