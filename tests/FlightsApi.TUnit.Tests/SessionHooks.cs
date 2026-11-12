namespace TonysTunit.Flights.TUnitTests;

/// <summary>
/// Session-level cleanup. TUnit requires these hooks to be static.
/// </summary>
public static class SessionHooks
{
    [After(TestSession)]
    public static void WriteSessionSummary()
    {
        Console.WriteLine("TUnit session finished.");
    }
}
