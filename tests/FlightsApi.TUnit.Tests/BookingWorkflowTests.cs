using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TonysTunit.Flights.Models;
using TonysTunit.Flights.TUnitTests.Support;

namespace TonysTunit.Flights.TUnitTests;

/// <summary>
/// A short booking workflow that uses <c>[DependsOn]</c>. The second test
/// waits for the first, then reads the booking id from the first test's
/// state bag. Isolated tests are still the better default; this shows the
/// feature for the comparison post.
/// </summary>
[ClassDataSource<FlightsWebFactory>(Shared = SharedType.PerTestSession)]
public sealed class BookingWorkflowTests(FlightsWebFactory factory)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [Test]
    public async Task Step1_Schedule_And_Book()
    {
        using var client = factory.CreateJsonClient();
        var flight = await client.ScheduleFlightAsync("AF801", seats: 8);
        var response = await client.BookAsync(flight.Id, email: $"workflow-{Guid.NewGuid():N}@example.com");
        var booking = await response.Content.ReadFromJsonAsync<Booking>(Json);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
        TestContext.Current!.StateBag.Items["BookingId"] = booking!.Id;
    }

    [Test]
    [DependsOn(nameof(Step1_Schedule_And_Book))]
    public async Task Step2_Cancel_The_Booking()
    {
        var step1 = TestContext.Current!.Dependencies.GetTests(nameof(Step1_Schedule_And_Book)).First();
        var bookingId = (Guid)step1.StateBag.Items["BookingId"]!;

        using var client = factory.CreateJsonClient();
        var cancel = await client.DeleteAsync($"/bookings/{bookingId}");
        var lookup = await client.GetAsync($"/bookings/{bookingId}");

        await Assert.That(cancel.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
        await Assert.That(lookup.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}
