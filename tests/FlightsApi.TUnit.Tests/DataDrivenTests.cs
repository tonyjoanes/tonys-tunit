using TonysTunit.Flights.Services;

namespace TonysTunit.Flights.TUnitTests;

public sealed class DataDrivenTests
{
    [Test]
    [Arguments("lhr", "LHR")]
    [Arguments("Jfk", "JFK")]
    [Arguments("  ams  ", "AMS")]
    public async Task Airport_Codes_Are_Normalised(string input, string expected)
    {
        await Assert.That(BookingService.NormaliseAirport(input)).IsEqualTo(expected);
    }

    [Test]
    [MethodDataSource(nameof(ValidFlightNumbers))]
    public async Task Valid_Flight_Numbers_Match_The_Expected_Shape(string flightNumber, bool isValid)
    {
        var matches = System.Text.RegularExpressions.Regex.IsMatch(flightNumber, @"^[A-Z]{2,3}\d{1,4}$");
        await Assert.That(matches).IsEqualTo(isValid);
    }

    public static IEnumerable<(string FlightNumber, bool IsValid)> ValidFlightNumbers()
    {
        yield return ("AF1", true);
        yield return ("AF123", true);
        yield return ("BA2490", true);
        yield return ("af123", false);
        yield return ("A1", false);
        yield return ("FLIGHT", false);
    }

    [Test]
    [MatrixDataSource]
    public async Task Airport_Pairs_Normalise_In_Every_Combination(
        [Matrix("lhr", "LHR", "Lhr")] string origin,
        [Matrix("jfk", "JFK")] string destination)
    {
        await Assert.That(BookingService.NormaliseAirport(origin)).IsEqualTo("LHR");
        await Assert.That(BookingService.NormaliseAirport(destination)).IsEqualTo("JFK");
        await Assert.That(origin.Trim().Equals(destination.Trim(), StringComparison.OrdinalIgnoreCase)).IsFalse();
    }
}
