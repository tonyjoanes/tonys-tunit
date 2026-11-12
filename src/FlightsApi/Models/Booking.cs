namespace TonysTunit.Flights.Models;

public sealed class Booking
{
    public Guid Id { get; set; }
    public required string Reference { get; set; }
    public Guid FlightId { get; set; }
    public required string PassengerName { get; set; }
    public required string PassengerEmail { get; set; }
    public int Seats { get; set; }
    public DateTimeOffset BookedAt { get; set; }
    public Flight? Flight { get; set; }
}
