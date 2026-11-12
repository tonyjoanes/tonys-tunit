namespace TonysTunit.Flights.Models;

public abstract record BookingResult
{
    public sealed record Success(Booking Booking) : BookingResult;
    public sealed record FlightNotFound : BookingResult;
    public sealed record NotEnoughSeats(int Available) : BookingResult;
    public sealed record DepartureInThePast : BookingResult;
}
