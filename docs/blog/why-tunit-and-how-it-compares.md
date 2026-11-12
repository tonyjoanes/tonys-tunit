# Why TUnit, and how it compares with xUnit

## Short version

If your integration tests are slow because each class spins up its own API, or they are full of fixture ceremony, TUnit makes that simpler: you start one test server for the whole run and point every test at it. The same idea is the strongest reason to look at it if you want Aspire or Playwright end-to-end tests against one running app. If your suite is mostly fast unit tests and xUnit already works for you, stay on xUnit.

This repo is a small flights Web API with two test projects covering the same ground: [TUnit](https://tunit.dev/) 1.83.8, and xUnit.net v3 4.0.3.

Figures checked on 12 November 2026: [8,312,911 NuGet downloads](https://www.nuget.org/packages/TUnit), [3,990 GitHub stars](https://github.com/thomhurst/TUnit), weekly downloads on [NuGet Trends](https://nugettrends.com/packages?ids=TUnit) of 232,793 in the week to 9 November against 134,164 in the week to 11 May, and latest stable 1.83.8 on 11 November 2026. It is used by [Refit](https://github.com/reactiveui/refit), [ReactiveUI](https://github.com/reactiveui/ReactiveUI), [Verify](https://github.com/VerifyTests/Verify) and [Shouldly](https://github.com/shouldly/shouldly). That is enough traction to take seriously, and still a younger ecosystem than xUnit.

## Why those differences matter

**Starting a whole app once, then pointing every end-to-end test at it.** An Aspire AppHost, or a Playwright browser suite, is expensive to start once per test class. [TUnit.Aspire](https://tunit.dev/docs/examples/aspire.md) (first published on 15 February 2026, [150,481 downloads](https://www.nuget.org/packages/TUnit.Aspire) as of 12 November 2026) starts the AppHost once for the session, waits for resources, and tears it down at the end. [TUnit.Playwright](https://tunit.dev/docs/examples/playwright.md) does the same for a browser. This repo does not include an Aspire or Playwright example: there is no AppHost here and no browser under test. The flights API tests use the same sharing pattern, on a single in-process server.

**An API spun up once per class.** In xUnit, an `IClassFixture` or collection fixture often means each class, or each collection, pays to start `WebApplicationFactory`. In TUnit you mark the class with `[ClassDataSource<FlightsWebFactory>(Shared = SharedType.PerTestSession)]` and one server lasts the whole run. That is what `BookingApiTests` does, via [TUnit.AspNetCore](https://tunit.dev/docs/examples/aspnet.md)'s `TestWebApplicationFactory`.

**A flaky end-to-end test in CI.** A network blip should not fail the build, and a hung call should not sit there. TUnit has first-party `[Retry]` and `[Timeout]`, at method, class or assembly level, which `ParallelismAndResilienceTests` shows. xUnit has a timeout on `[Fact]`, but no first-party retry.

**An expensive multi-step flow.** Sometimes you want "book, then cancel" as two tests, with the second waiting for the first, without making the rest of the suite run in a line. `[DependsOn]` does that, and the second test can read the first test's state bag, as `BookingWorkflowTests` shows. Isolated tests are still the better default, as TUnit's own docs say. xUnit has no equivalent, so that project folds both steps into one test.

**Set-up and tear-down that should span the whole run.** xUnit's hooks stop at the class, or need a fixture class at assembly scope. TUnit has `[Before]` and `[After]` at test, class, assembly, session and discovery: `HooksTests` uses `[Before(Class)]`, and `SessionHooks` uses `[After(TestSession)]` once at the end of the run.

**A broken hook that only fails in CI.** TUnit [discovers tests at compile time](https://tunit.dev/docs/getting-started/installation.md) with a source generator. A bad hook signature or a broken data source fails the build, not a later test run, and that is also how it reaches Native AOT and trimmed, single-file test apps. It is built only on [Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro): it does not support VSTest, and you should not add `Microsoft.NET.Test.Sdk`. xUnit v3 can also run on MTP (the default from the 4.0 line, and what the xUnit project here uses), but TUnit was designed around that platform from the start.

Tests also [run in parallel by default](https://tunit.dev/docs/execution/parallelism.md), with `[NotInParallel]` when needed, and [assertions](https://tunit.dev/docs/migration/xunit.md) are async and fluent.

## Side by side

A theory, written both ways:

```csharp
// xUnit v3
[Theory]
[InlineData("lhr", "LHR")]
[InlineData("Jfk", "JFK")]
public void Airport_Codes_Are_Normalised(string input, string expected)
{
    Assert.Equal(expected, BookingService.NormaliseAirport(input));
}

// TUnit
[Test]
[Arguments("lhr", "LHR")]
[Arguments("Jfk", "JFK")]
public async Task Airport_Codes_Are_Normalised(string input, string expected)
{
    await Assert.That(BookingService.NormaliseAirport(input)).IsEqualTo(expected);
}
```

`[MemberData]` becomes `[MethodDataSource]`, returning tuples instead of `object[]`. TUnit also has `[Matrix]`, which builds every combination of per-parameter values. xUnit has no built-in equivalent, so this repo lists the same six airport-pair cases as `[InlineData]` rows.

Sharing a `WebApplicationFactory` for the session looks like this:

```csharp
// TUnit
[ClassDataSource<FlightsWebFactory>(Shared = SharedType.PerTestSession)]
public sealed class BookingApiTests(FlightsWebFactory factory) { /* tests */ }

// xUnit v3
[CollectionDefinition(nameof(FlightsApiCollection))]
public sealed class FlightsApiCollection : ICollectionFixture<FlightsWebFactory>;

[Collection(nameof(FlightsApiCollection))]
public sealed class BookingApiTests(FlightsWebFactory factory) { /* tests */ }
```

From TUnit's [migration guide](https://tunit.dev/docs/migration/xunit.md):

| xUnit | TUnit |
| --- | --- |
| `[Fact]` / `[Theory]` | `[Test]` |
| `[InlineData]` | `[Arguments]` |
| `[MemberData]` | `[MethodDataSource]` |
| `IClassFixture<T>` | `[ClassDataSource<T>(Shared = SharedType.PerClass)]` |
| `[Collection]` / `ICollectionFixture<T>` | `[ClassDataSource<T>(Shared = SharedType.Keyed)]` |
| Constructor / `IAsyncLifetime` | Constructor or `[Before(Test)]` |
| `ITestOutputHelper` | `TestContext` |
| `Assert.Equal(expected, actual)` | `await Assert.That(actual).IsEqualTo(expected)` |

## How the four frameworks compare

NUnit and MSTest notes come from TUnit's [framework differences](https://tunit.dev/docs/comparison/framework-differences.md) page and Microsoft's [testing platform overview](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro).

| | TUnit | xUnit v3 | NUnit | MSTest |
| --- | --- | --- | --- | --- |
| Test method | `[Test]` | `[Fact]` / `[Theory]` | `[Test]` | `[TestMethod]` |
| Discovery | Source generators | Reflection, with AOT work in v3 | Reflection | Reflection |
| Default runner | Microsoft.Testing.Platform only | MTP (default in 4.x) and VSTest | VSTest plus an MTP runner | VSTest plus an MTP runner |
| Parallel by default | Yes, every test | Collections in parallel; tests in a collection sequential | Opt-in | Opt-in |
| Native AOT | First-class, via source generation | AOT column in TUnit's Nov 2026 benchmarks | Not first-class | Not first-class |
| Async assertions | Built in | Classic `Assert`, mostly synchronous | Constraint model | Classic `Assert` |
| Shared fixtures | `ClassDataSource` and a `Shared` mode | `IClassFixture`, `ICollectionFixture` | `OneTimeSetUp`, or a fixture class | `ClassInitialize` |
| Session hooks | `[Before(TestSession)]` / `[After(TestSession)]` | No direct equivalent | `SetUpFixture` is assembly-scoped | `AssemblyInitialize` |
| Test dependencies | `[DependsOn]` | None | `[Order]` | None |
| Retry / timeout | `[Retry]`, `[Timeout]`, at any level | Timeout on `[Fact]`; no first-party retry | `[Retry]`, `[Timeout]` | Present in recent versions |
| Matrix data | `[Matrix]` | Manual rows or a third-party package | `[Values]` / `[TestCase]` | `[DataRow]` |
| Aspire / Playwright | `TUnit.Aspire`, `TUnit.Playwright` | Community or vendor packages | Playwright has an NUnit package | Playwright has an MSTest package |

NUnit's default of one instance per class, with state leaking between tests, is a well known trap. TUnit, like xUnit, creates a new instance for every test. NUnit's constraint assertions inspired TUnit's fluent style, but TUnit pushes the type checking to the compiler. MSTest is a solid choice if your team is already on it. Neither is the focus here.

## When you would stay on xUnit

xUnit is the default in almost every .NET tutorial, template and sample, and its ecosystem (analysers, IDE runners, coverage tools, company standards) is mature. If your team already writes fluent xUnit and your CI is quiet, a migration has no obvious payoff. Try TUnit in your actual workflow before you commit: some older VSTest-only tools will not work, and Coverlet is [incompatible with TUnit](https://tunit.dev/docs/getting-started/installation.md) (TUnit ships Microsoft's coverage extension instead). Familiarity matters as much as features. xUnit v3 has also closed some of the gap (MTP by default, a timeout on facts, even an AOT path), so you can take the new runner without changing framework.

## If you do migrate

TUnit ships [Roslyn analysers and a code fixer](https://tunit.dev/docs/migration/xunit.md) (`TUXU0001`) that rewrite most of a typical suite: `[Fact]` and `[Theory]` to `[Test]`, `[InlineData]` to `[Arguments]`, `[MemberData]` to `[MethodDataSource]`, and the common assertions to the fluent form. Add TUnit alongside xUnit, turn off TUnit's implicit usings so the two `Assert` types do not clash, run `dotnet format analyzers --severity info --diagnostics TUXU0001`, then finish by hand. The fixer will not convert fixtures, `IAsyncLifetime` or `ITestOutputHelper`, and data methods should return tuples rather than `object[]`. Remove the xUnit packages, confirm with `dotnet run -- --list-tests`, and migrate a single project first.

## A note on speed

TUnit [publishes weekly benchmarks](https://tunit.dev/docs/benchmarks) against xUnit v3, NUnit and MSTest, including Native AOT runs. Those are the numbers to look at if performance is your reason for switching. This repo is not that. On 12 November 2026, a Release `dotnet test` run here reported 2.083 seconds for 33 TUnit tests and 2.287 seconds for 32 xUnit tests, including starting an in-process test server. A suite this small proves almost nothing about speed.

## Try it

The code lives in [tonyjoanes/tonys-tunit](https://github.com/tonyjoanes/tonys-tunit). `dotnet test TonysTunit.slnx` runs both projects. If you already have an xUnit suite, try the migration guide on one of your own projects.
