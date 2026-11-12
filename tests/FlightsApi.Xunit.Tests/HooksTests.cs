using TonysTunit.Flights.Services;

namespace TonysTunit.Flights.XunitTests;

/// <summary>
/// The closest xUnit equivalent of TUnit's <c>[Before(Class)]</c> is
/// <see cref="IClassFixture{TFixture}"/>. The fixture is constructed once
/// for the class. There is no first-party session-level hook comparable to
/// <c>[After(TestSession)]</c>; v3's assembly fixtures still need a fixture
/// class rather than a single attributed method.
/// </summary>
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
