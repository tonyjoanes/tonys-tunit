namespace TonysTunit.Flights.Models;

public sealed class Flight
{
    public Guid Id { get; set; }
    public required string FlightNumber { get; set; }
    public required string Origin { get; set; }
    public required string Destination { get; set; }
    public DateTimeOffset Departure { get; set; }
    public int SeatsAvailable { get; set; }
}
