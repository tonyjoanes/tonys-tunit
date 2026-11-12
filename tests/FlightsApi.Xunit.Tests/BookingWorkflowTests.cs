// Matches TUnit's BookingWorkflowTests.cs. xUnit has no [DependsOn], so
// the book-then-cancel workflow is a single test. That keeps the steps
// ordered without turning off parallelism for the rest of the class.

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TonysTunit.Flights.Models;
using TonysTunit.Flights.XunitTests.Support;

namespace TonysTunit.Flights.XunitTests;

[Collection(nameof(FlightsApiCollection))]
public sealed class BookingWorkflowTests(FlightsWebFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Schedule_Book_Then_Cancel()
    {
        using var client = factory.CreateJsonClient();
        var flight = await client.ScheduleFlightAsync("XF801", seats: 8);

        var booked = await client.BookAsync(flight.Id, email: $"workflow-{Guid.NewGuid():N}@example.com");
        var booking = await booked.Content.ReadFromJsonAsync<Booking>(Json, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Created, booked.StatusCode);

        var cancel = await client.DeleteAsync($"/bookings/{booking!.Id}", TestContext.Current.CancellationToken);
        var lookup = await client.GetAsync($"/bookings/{booking.Id}", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, cancel.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, lookup.StatusCode);
    }
}
