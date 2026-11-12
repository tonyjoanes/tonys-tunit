// Demonstrates [Before(Class)], which runs once before the first test
// in the class.
// Problem: shared set-up that should happen once, written as a method
// rather than a fixture class.
// xUnit equivalent: HooksTests.cs, which uses IClassFixture<AirportClassFixture>.

using TonysTunit.Flights.Services;

namespace TonysTunit.Flights.TUnitTests;

public sealed class HooksTests
{
    private static int _classSetUpCount;
    private static string? _normalised;

    [Before(Class)]
    public static void PrepareSharedData()
    {
        _classSetUpCount++;
        _normalised = BookingService.NormaliseAirport("  lhr ");
    }

    [Test]
    public async Task Class_Hook_Ran_Once_Before_The_First_Test()
    {
        await Assert.That(_classSetUpCount).IsEqualTo(1);
        await Assert.That(_normalised).IsEqualTo("LHR");
    }

    [Test]
    public async Task Class_Hook_Is_Not_Repeated_For_The_Second_Test()
    {
        await Assert.That(_classSetUpCount).IsEqualTo(1);
    }
}
