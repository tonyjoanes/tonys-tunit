using System.ComponentModel.DataAnnotations;

namespace TonysTunit.Flights.Models;

public sealed class CreateBookingRequest
{
    [Required]
    public Guid FlightId { get; init; }

    [Required]
    [MinLength(2)]
    [MaxLength(100)]
    public required string PassengerName { get; init; }

    [Required]
    [EmailAddress]
    public required string PassengerEmail { get; init; }

    [Range(1, 9)]
    public int Seats { get; init; } = 1;
}
