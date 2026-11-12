namespace TonysTunit.Flights.XunitTests;

/// <summary>
/// Tests in the same xUnit class run sequentially by default, so a shared
/// counter is safe here without an extra attribute. There is no first-party
/// <c>[Retry]</c>; a timeout is set on the fact itself.
/// </summary>
public sealed class ParallelismAndResilienceTests
{
    private static int _seatHold;

    [Fact]
    public void First_Writer_Increments_The_Shared_Hold()
    {
        var before = _seatHold;
        _seatHold = before + 1;
        Assert.Equal(before + 1, _seatHold);
    }

    [Fact]
    public void Second_Writer_Increments_The_Shared_Hold()
    {
        var before = _seatHold;
        _seatHold = before + 1;
        Assert.Equal(before + 1, _seatHold);
    }

    [Fact(Timeout = 5_000)]
    public async Task Lookup_Completes_Within_The_Timeout()
    {
        await Task.Delay(1, TestContext.Current.CancellationToken);
        Assert.True(true);
    }
}
