using System.Net.Http.Json;
using System.Text.Json;
using TonysTunit.Flights.Models;

namespace TonysTunit.Flights.TUnitTests.Support;

internal static class HttpHelpers
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public static async Task<Flight> ScheduleFlightAsync(
        this HttpClient client,
        string flightNumber,
        string origin = "LHR",
        string destination = "JFK",
        int seats = 20,
        DateTimeOffset? departure = null)
    {
        var request = new CreateFlightRequest
        {
            FlightNumber = flightNumber,
            Origin = origin,
            Destination = destination,
            Departure = departure ?? DateTimeOffset.UtcNow.AddDays(3),
            Seats = seats
        };

        var response = await client.PostAsJsonAsync("/flights", request, Json);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<Flight>(Json))!;
    }

    public static Task<HttpResponseMessage> BookAsync(
        this HttpClient client,
        Guid flightId,
        string name = "Ada Lovelace",
        string email = "ada@example.com",
        int seats = 1) =>
        client.PostAsJsonAsync("/bookings", new CreateBookingRequest
        {
            FlightId = flightId,
            PassengerName = name,
            PassengerEmail = email,
            Seats = seats
        }, Json);
}
