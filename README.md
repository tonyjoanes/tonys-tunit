# tony's tunit

A small .NET 10 flights Web API, tested twice: once with [TUnit](https://tunit.dev/) 1.83.8, and once with [xUnit.net v3](https://xunit.net/) 4.0.3, covering the same scenarios. There is a blog post in [`docs/blog/`](docs/blog/why-tunit-and-how-it-compares.md) that compares the two and explains when you might switch, or stay put.

## Why bother?

If your integration tests are slow because each class spins up its own API, or they are full of fixture ceremony, TUnit makes that simpler: you start one test server for the whole run and point every test at it. The same idea is the strongest reason to look at it if you want Aspire or Playwright end-to-end tests against one running app. If your suite is mostly fast unit tests and xUnit already works for you, stay on xUnit. This repo does not include an Aspire example; the flights API tests show the same sharing pattern on a single in-process server.

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

Each TUnit file is paired with a matching xUnit file of the same name (except `SessionHooks.cs`, which has no xUnit equivalent). The comments at the top of each file say the same thing in a bit more detail.

| TUnit file | Problem it shows |
| --- | --- |
| `BookingApiTests.cs` | The API is spun up once per class, so the suite is slow and full of fixture ceremony. TUnit shares one test server for the session. |
| `BookingWorkflowTests.cs` | An expensive multi-step flow that you want as separate tests, with a defined order. TUnit uses `[DependsOn]`; xUnit folds both steps into one test. |
| `ParallelismAndResilienceTests.cs` | Tests that share a resource must not overlap, and a flaky end-to-end call in CI should retry or time out. |
| `HooksTests.cs` | Shared set-up that should run once, written as a `[Before(Class)]` method rather than a fixture class. |
| `SessionHooks.cs` | Tear-down or a summary at the end of the whole run, with `[After(TestSession)]`. |
| `DataDrivenTests.cs` | Combinations of inputs, including a matrix, without copying test methods or listing every pair by hand. |
| `BookingServiceTests.cs` | Ordinary unit tests: the baseline "same test, different syntax" comparison. |

On this machine, 12 November 2026, `dotnet test TonysTunit.slnx --configuration Release --no-build` reported **65 passed, 0 failed** (33 TUnit, 32 xUnit). The TUnit project reported a duration of 2.083 seconds on a standalone run, and xUnit 2.287 seconds. Those figures include starting the in-process test server, and a suite this small is not a benchmark. [TUnit publishes its own comparisons](https://tunit.dev/docs/benchmarks) on much larger suites.

## What is not here

There is no Testcontainers example. Docker was not available on the machine that built this repo, so a containerised database would have been guesswork. [TUnit.AspNetCore](https://tunit.dev/docs/examples/aspnet.md) and [TUnit.Aspire](https://tunit.dev/docs/examples/aspire.md) both document the pattern (a container fixture with `Shared = SharedType.PerTestSession`) if you want to add one.

## Licence

[MIT](LICENSE).
