# tony's tunit

A small .NET 10 flights Web API, tested twice: once with [TUnit](https://tunit.dev/) 1.83.8, and once with [xUnit.net v3](https://xunit.net/) 4.0.3, covering the same scenarios. There is a blog post in [`docs/blog/`](docs/blog/why-tunit-and-how-it-compares.md) that compares the two and explains when you might switch, or stay put.

This is a learning repo, not a production service. The API uses an in-memory EF Core store, so it resets every time the process starts.

## Requirements

- The .NET 10 SDK. [`global.json`](global.json) pins **10.0.404**, which is what this repo was built and tested against.

## How to run

Restore, build and run both test projects:

```bash
dotnet test TonysTunit.slnx
```

Or run them separately, including with `dotnet run`, which is the natural entry point for Microsoft.Testing.Platform projects:

```bash
dotnet test tests/FlightsApi.TUnit.Tests
dotnet run --project tests/FlightsApi.TUnit.Tests

dotnet test tests/FlightsApi.Xunit.Tests
dotnet run --project tests/FlightsApi.Xunit.Tests
```

To run the API itself:

```bash
dotnet run --project src/FlightsApi
```

Then try `GET /health`, `GET /flights`, `POST /flights` and `POST /bookings`. Invalid input and missing resources return [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457).

## Project layout

```
src/FlightsApi/                  Minimal API, EF Core InMemory, BookingService
tests/FlightsApi.TUnit.Tests/    TUnit examples (the main point of the repo)
tests/FlightsApi.Xunit.Tests/    The same scenarios written in xUnit v3
docs/blog/                       The comparison post
.github/workflows/ci.yml         Build and test on ubuntu-latest
```

Package versions are centralised in [`Directory.Packages.props`](Directory.Packages.props).

## What the tests show

Both projects cover:

- Unit tests for `BookingService`, with a fake clock and a fixed booking-reference generator
- Data-driven cases (`[Arguments]` / `[InlineData]`, `[MethodDataSource]` / `[MemberData]`, and TUnit's `[Matrix]`, which xUnit spells out by hand)
- Integration tests against `WebApplicationFactory`, sharing one factory for the session (TUnit's `[ClassDataSource(Shared = SharedType.PerTestSession)]`, xUnit's `ICollectionFixture`)
- A short booking workflow. TUnit uses `[DependsOn]` across two tests; xUnit keeps the steps in a single test, because it has no equivalent
- Parallelism controls, retries and timeouts. TUnit has `[NotInParallel]`, `[Retry]` and `[Timeout]`. xUnit runs tests in a class sequentially by default and has a timeout on `[Fact]`, but no first-party retry
- Lifecycle hooks. TUnit uses `[Before(Class)]` and `[After(TestSession)]`. xUnit uses `IClassFixture`

On this machine, 12 November 2026, `dotnet test TonysTunit.slnx --configuration Release --no-build` reported **65 passed, 0 failed** (33 TUnit, 32 xUnit). The TUnit project reported a duration of 2.083 seconds on a standalone run, and xUnit 2.287 seconds. Those figures include starting the in-process test server, and a suite this small is not a benchmark. [TUnit publishes its own comparisons](https://tunit.dev/docs/benchmarks) on much larger suites.

## What is not here

There is no Testcontainers example. Docker was not available on the machine that built this repo, so a containerised database would have been guesswork. [TUnit.AspNetCore](https://tunit.dev/docs/examples/aspnet.md) and [TUnit.Aspire](https://tunit.dev/docs/examples/aspire.md) both document the pattern (a container fixture with `Shared = SharedType.PerTestSession`) if you want to add one.

## Licence

[MIT](LICENSE).
