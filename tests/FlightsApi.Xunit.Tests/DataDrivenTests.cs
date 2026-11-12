// Matches TUnit's DataDrivenTests.cs. [InlineData] and [MemberData] stand
// in for [Arguments] and [MethodDataSource]. xUnit has no built-in
// [Matrix], so the six airport-pair cases are listed as [InlineData] rows.

using TonysTunit.Flights.Services;

namespace TonysTunit.Flights.XunitTests;

public sealed class DataDrivenTests
{
    [Theory]
    [InlineData("lhr", "LHR")]
    [InlineData("Jfk", "JFK")]
    [InlineData("  ams  ", "AMS")]
    public void Airport_Codes_Are_Normalised(string input, string expected)
    {
        Assert.Equal(expected, BookingService.NormaliseAirport(input));
    }

    [Theory]
    [MemberData(nameof(ValidFlightNumbers))]
    public void Valid_Flight_Numbers_Match_The_Expected_Shape(string flightNumber, bool isValid)
    {
        var matches = System.Text.RegularExpressions.Regex.IsMatch(flightNumber, @"^[A-Z]{2,3}\d{1,4}$");
        Assert.Equal(isValid, matches);
    }

    public static TheoryData<string, bool> ValidFlightNumbers =>
        new()
        {
            { "AF1", true },
            { "AF123", true },
            { "BA2490", true },
            { "af123", false },
            { "A1", false },
            { "FLIGHT", false }
        };

    // xUnit has no built-in matrix attribute. The six cases below are the
    // same combinations TUnit generates from [Matrix] in this repo.
    [Theory]
    [InlineData("lhr", "jfk")]
    [InlineData("lhr", "JFK")]
    [InlineData("LHR", "jfk")]
    [InlineData("LHR", "JFK")]
    [InlineData("Lhr", "jfk")]
    [InlineData("Lhr", "JFK")]
    public void Airport_Pairs_Normalise_In_Every_Combination(string origin, string destination)
    {
        Assert.Equal("LHR", BookingService.NormaliseAirport(origin));
        Assert.Equal("JFK", BookingService.NormaliseAirport(destination));
        Assert.False(origin.Trim().Equals(destination.Trim(), StringComparison.OrdinalIgnoreCase));
    }
}
