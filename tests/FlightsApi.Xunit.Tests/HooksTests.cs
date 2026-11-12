// Matches TUnit's HooksTests.cs and SessionHooks.cs. The closest xUnit
// equivalent of [Before(Class)] is IClassFixture, constructed once for
// the class. There is no first-party session hook comparable to
// [After(TestSession)]; v3 assembly fixtures still need a fixture class
// rather than a single attributed method.

using TonysTunit.Flights.Services;

namespace TonysTunit.Flights.XunitTests;

public sealed class AirportClassFixture
{
    public AirportClassFixture()
    {
        SetUpCount++;
        Normalised = BookingService.NormaliseAirport("  lhr ");
    }

    public string Normalised { get; }

    public static int SetUpCount { get; private set; }
}

public sealed class HooksTests(AirportClassFixture fixture) : IClassFixture<AirportClassFixture>
{
    [Fact]
    public void Class_Fixture_Ran_Once_Before_The_First_Test()
    {
        Assert.Equal(1, AirportClassFixture.SetUpCount);
        Assert.Equal("LHR", fixture.Normalised);
    }

    [Fact]
    public void Class_Fixture_Is_Not_Repeated_For_The_Second_Test()
    {
        Assert.Equal(1, AirportClassFixture.SetUpCount);
        Assert.Equal("LHR", fixture.Normalised);
    }
}
