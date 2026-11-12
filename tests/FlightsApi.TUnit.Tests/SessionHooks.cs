// Demonstrates [After(TestSession)] for a summary (or clean-up) at the
// end of the whole run. TUnit requires these hooks to be static.
// Problem: set-up and tear-down that should span the entire test session,
// not a single class.
// xUnit equivalent: there is no matching file. xUnit has no first-party
// session hook like [After(TestSession)]; HooksTests.cs notes this.

namespace TonysTunit.Flights.TUnitTests;

public static class SessionHooks
{
    [After(TestSession)]
    public static void WriteSessionSummary()
    {
        Console.WriteLine("TUnit session finished.");
    }
}
