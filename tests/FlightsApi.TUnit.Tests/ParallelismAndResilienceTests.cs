// Demonstrates [NotInParallel], [Retry] and [Timeout]. The shared counter
// is only safe because both writer tests share a constraint key and
// therefore never overlap.
// Problem: tests that share a resource must not run at the same time, and
// a flaky end-to-end call in CI should retry (or time out) without a
// home-grown helper.
// xUnit equivalent: ParallelismAndResilienceTests.cs, which relies on
// xUnit's default of sequential tests in a class, and [Fact(Timeout = ...)].
// There is no first-party [Retry].

namespace TonysTunit.Flights.TUnitTests;

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
