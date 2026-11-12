using Microsoft.EntityFrameworkCore;
using TonysTunit.Flights.Data;
using TonysTunit.Flights.Models;

namespace TonysTunit.Flights.Services;

public sealed class BookingService(
    FlightsDbContext db,
    IBookingReferenceGenerator references,
    TimeProvider clock)
{
    public async Task<Flight> ScheduleAsync(CreateFlightRequest request, CancellationToken cancellationToken = default)
    {
        var origin = NormaliseAirport(request.Origin);
        var destination = NormaliseAirport(request.Destination);
        var flightNumber = request.FlightNumber.Trim().ToUpperInvariant();

        var flight = new Flight
        {
            Id = Guid.CreateVersion7(),
            FlightNumber = flightNumber,
            Origin = origin,
            Destination = destination,
            Departure = request.Departure,
            SeatsAvailable = request.Seats
        };

        db.Flights.Add(flight);
        await db.SaveChangesAsync(cancellationToken);
        return flight;
    }

    public Task<Flight?> GetFlightAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Flights.AsNoTracking().FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Flight>> ListFlightsAsync(CancellationToken cancellationToken = default) =>
        await db.Flights.AsNoTracking().OrderBy(f => f.Departure).ToListAsync(cancellationToken);

    public async Task<BookingResult> BookAsync(CreateBookingRequest request, CancellationToken cancellationToken = default)
    {
        var flight = await db.Flights.FirstOrDefaultAsync(f => f.Id == request.FlightId, cancellationToken);
        if (flight is null)
        {
            return new BookingResult.FlightNotFound();
        }

        if (flight.Departure <= clock.GetUtcNow())
        {
            return new BookingResult.DepartureInThePast();
        }

        if (flight.SeatsAvailable < request.Seats)
        {
            return new BookingResult.NotEnoughSeats(flight.SeatsAvailable);
        }

        flight.SeatsAvailable -= request.Seats;

        var booking = new Booking
        {
            Id = Guid.CreateVersion7(),
            Reference = references.Next(),
            FlightId = flight.Id,
            PassengerName = request.PassengerName.Trim(),
            PassengerEmail = request.PassengerEmail.Trim().ToLowerInvariant(),
            Seats = request.Seats,
            BookedAt = clock.GetUtcNow()
        };

        db.Bookings.Add(booking);
        await db.SaveChangesAsync(cancellationToken);
        return new BookingResult.Success(booking);
    }

    public Task<Booking?> GetBookingAsync(Guid id, CancellationToken cancellationToken = default) =>
        db.Bookings.AsNoTracking().Include(b => b.Flight).FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

    public async Task<bool> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var booking = await db.Bookings.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);
        if (booking is null)
        {
            return false;
        }

        var flight = await db.Flights.FirstOrDefaultAsync(f => f.Id == booking.FlightId, cancellationToken);
        if (flight is not null)
        {
            flight.SeatsAvailable += booking.Seats;
        }

        db.Bookings.Remove(booking);
        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public static string NormaliseAirport(string code) => code.Trim().ToUpperInvariant();
}
