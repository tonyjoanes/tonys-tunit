using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TonysTunit.Flights.Models;
using TonysTunit.Flights.XunitTests.Support;

namespace TonysTunit.Flights.XunitTests;

[Collection(nameof(FlightsApiCollection))]
public sealed class BookingApiTests(FlightsWebFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Health_Returns_Ok()
    {
        using var client = factory.CreateJsonClient();

        var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Seeded_Flights_Are_Listed()
    {
        using var client = factory.CreateJsonClient();

        var flights = await client.GetFromJsonAsync<List<Flight>>("/flights", Json, TestContext.Current.CancellationToken);

        Assert.NotNull(flights);
        Assert.Contains(flights, f => f.FlightNumber == "AF101");
    }

    [Fact]
    public async Task Missing_Flight_Returns_ProblemDetails()
    {
        using var client = factory.CreateJsonClient();
        var id = Guid.CreateVersion7();

        var response = await client.GetAsync($"/flights/{id}", TestContext.Current.CancellationToken);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotNull(problem);
        Assert.Equal("Flight not found", problem.Title);
        Assert.Contains(id.ToString(), problem.Detail);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Invalid_Booking_Returns_A_Validation_Problem()
    {
        using var client = factory.CreateJsonClient();

        var response = await client.PostAsJsonAsync("/bookings", new
        {
            flightId = Guid.CreateVersion7(),
            passengerName = "A",
            passengerEmail = "not-an-email",
            seats = 0
        }, Json, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Booking_A_New_Flight_Returns_Created()
    {
        using var client = factory.CreateJsonClient();
        var flight = await client.ScheduleFlightAsync("XF701", seats: 12);
        var email = $"ada-{Guid.NewGuid():N}@example.com";

        var response = await client.BookAsync(flight.Id, email: email, seats: 2);
        var booking = await response.Content.ReadFromJsonAsync<Booking>(Json, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(booking);
        Assert.Equal(email, booking.PassengerEmail);
        Assert.Equal(2, booking.Seats);
        Assert.StartsWith("AF-", booking.Reference);
        Assert.Equal($"/bookings/{booking.Id}", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Overbooking_Returns_Conflict_ProblemDetails()
    {
        using var client = factory.CreateJsonClient();
        var flight = await client.ScheduleFlightAsync("XF702", seats: 1);

        var response = await client.BookAsync(flight.Id, email: $"full-{Guid.NewGuid():N}@example.com", seats: 2);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("Not enough seats", problem!.Title);
        Assert.Contains("1 seat", problem.Detail);
    }
}
