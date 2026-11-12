namespace TonysTunit.Flights.TUnitTests;

/// <summary>
/// Demonstrates <c>[NotInParallel]</c>, <c>[Retry]</c> and <c>[Timeout]</c>.
/// The shared counter is only safe because both tests share a constraint key
/// and therefore never overlap.
/// </summary>
public sealed class ParallelismAndResilienceTests
{
    private static int _seatHold;

    [Test]
    [NotInParallel("seat-hold")]
    public async Task First_Writer_Increments_The_Shared_Hold()
    {
        var before = _seatHold;
        _seatHold = before + 1;
        await Assert.That(_seatHold).IsEqualTo(before + 1);
    }

    [Test]
    [NotInParallel("seat-hold")]
    public async Task Second_Writer_Increments_The_Shared_Hold()
    {
        var before = _seatHold;
        _seatHold = before + 1;
        await Assert.That(_seatHold).IsEqualTo(before + 1);
    }

    [Test]
    [Retry(2)]
    [Timeout(5_000)]
    public async Task Lookup_Completes_Within_The_Timeout(CancellationToken cancellationToken)
    {
        // Always succeeds on the first attempt. The attributes are here to
        // show the built-in retry and timeout APIs, not to paper over a
        // flaky test.
        await Task.Delay(1, cancellationToken);
        await Assert.That(Environment.ProcessorCount).IsGreaterThan(0);
    }
}
