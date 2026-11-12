using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using TonysTunit.Flights.Models;
using TonysTunit.Flights.TUnitTests.Support;

namespace TonysTunit.Flights.TUnitTests;

[ClassDataSource<FlightsWebFactory>(Shared = SharedType.PerTestSession)]
public sealed class BookingApiTests(FlightsWebFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Test]
    public async Task Health_Returns_Ok()
    {
        using var client = factory.CreateJsonClient();

        var response = await client.GetAsync("/health");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Seeded_Flights_Are_Listed()
    {
        using var client = factory.CreateJsonClient();

        var flights = await client.GetFromJsonAsync<List<Flight>>("/flights", Json);

        await Assert.That(flights).IsNotNull();
        await Assert.That(flights!.Select(f => f.FlightNumber)).Contains("AF101");
    }

    [Test]
    public async Task Missing_Flight_Returns_ProblemDetails()
    {
        using var client = factory.CreateJsonClient();
        var id = Guid.CreateVersion7();

        var response = await client.GetAsync($"/flights/{id}");
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
        await Assert.That(problem).IsNotNull();
        await Assert.That(problem!.Title).IsEqualTo("Flight not found");
        await Assert.That(problem.Detail).Contains(id.ToString());
        await Assert.That(response.Content.Headers.ContentType!.MediaType)
            .IsEqualTo("application/problem+json");
    }

    [Test]
    public async Task Invalid_Booking_Returns_A_Validation_Problem()
    {
        using var client = factory.CreateJsonClient();

        var response = await client.PostAsJsonAsync("/bookings", new
        {
            flightId = Guid.CreateVersion7(),
            passengerName = "A",
            passengerEmail = "not-an-email",
            seats = 0
        }, Json);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
        await Assert.That(response.Content.Headers.ContentType!.MediaType)
            .IsEqualTo("application/problem+json");
    }

    [Test]
    public async Task Booking_A_New_Flight_Returns_Created()
    {
        using var client = factory.CreateJsonClient();
        var flight = await client.ScheduleFlightAsync("AF701", seats: 12);
        var email = $"ada-{Guid.NewGuid():N}@example.com";

        var response = await client.BookAsync(flight.Id, email: email, seats: 2);
        var booking = await response.Content.ReadFromJsonAsync<Booking>(Json);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(booking).IsNotNull();
        await Assert.That(booking!.PassengerEmail).IsEqualTo(email);
        await Assert.That(booking.Seats).IsEqualTo(2);
        await Assert.That(booking.Reference).StartsWith("AF-");
        await Assert.That(response.Headers.Location!.ToString()).IsEqualTo($"/bookings/{booking.Id}");
    }

    [Test]
    public async Task Overbooking_Returns_Conflict_ProblemDetails()
    {
        using var client = factory.CreateJsonClient();
        var flight = await client.ScheduleFlightAsync("AF702", seats: 1);

        var response = await client.BookAsync(flight.Id, email: $"full-{Guid.NewGuid():N}@example.com", seats: 2);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Json);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
        await Assert.That(problem!.Title).IsEqualTo("Not enough seats");
        await Assert.That(problem.Detail).Contains("1 seat");
    }
}
