using System.ComponentModel.DataAnnotations;

namespace TonysTunit.Flights.Models;

public sealed class CreateFlightRequest
{
    [Required]
    [RegularExpression(@"^[A-Z]{2,3}\d{1,4}$", ErrorMessage = "Flight number must look like AF123.")]
    public required string FlightNumber { get; init; }

    [Required]
    [RegularExpression(@"^[A-Za-z]{3}$", ErrorMessage = "Origin must be a three-letter airport code.")]
    public required string Origin { get; init; }

    [Required]
    [RegularExpression(@"^[A-Za-z]{3}$", ErrorMessage = "Destination must be a three-letter airport code.")]
    public required string Destination { get; init; }

    [Required]
    public required DateTimeOffset Departure { get; init; }

    [Range(1, 400)]
    public int Seats { get; init; } = 180;
}
