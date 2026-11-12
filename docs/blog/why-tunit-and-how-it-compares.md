# Why TUnit, and how it compares with xUnit

I have been looking at [TUnit](https://tunit.dev/), a modern .NET testing framework by Tom Longhurst, because it has been picking up real use this year. This repo is a small flights Web API with two test projects that cover the same ground: one in TUnit 1.83.8, and one in xUnit.net v3 4.0.3. The aim is to show, in code you can run, where the two frameworks feel different and when a move might be worth it.

Figures below were checked on 12 November 2026. TUnit had [8,312,911 NuGet downloads](https://www.nuget.org/packages/TUnit) and [3,990 GitHub stars](https://github.com/thomhurst/TUnit). Weekly downloads on [NuGet Trends](https://nugettrends.com/packages?ids=TUnit) were 232,793 in the week to 9 November, against 134,164 in the week to 11 May. The latest stable release, 1.83.8, was published on 11 November 2026. It is used in the test suites of [Refit](https://github.com/reactiveui/refit), [ReactiveUI](https://github.com/reactiveui/ReactiveUI), [Verify](https://github.com/VerifyTests/Verify) and [Shouldly](https://github.com/shouldly/shouldly). That is enough traction to take it seriously, but it is still a younger ecosystem than xUnit.

## Why TUnit is interesting

TUnit discovers tests at compile time with a [source generator](https://tunit.dev/docs/getting-started/installation.md), rather than by reflecting over assemblies at runtime. The practical effects are faster startup, mistakes such as a bad hook signature failing the build instead of a CI run, and a path to Native AOT and trimmed, single-file test apps. It is built only on [Microsoft.Testing.Platform](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro): it does not support VSTest, and you should not add `Microsoft.NET.Test.Sdk`. xUnit v3 can also run on MTP (the default from the 4.0 line, and what the xUnit project here uses), but TUnit was designed around the new platform from the start.

Tests [run in parallel by default](https://tunit.dev/docs/execution/parallelism.md), capped at twice the processor count. `[NotInParallel]`, `[ParallelGroup]` and `[ParallelLimiter]` control actual concurrent tests, not just thread count. Assertions are [async and fluent](https://tunit.dev/docs/migration/xunit.md): `await Assert.That(actual).IsEqualTo(expected)` reads more clearly than `Assert.Equal(expected, actual)`, and is type-aware, so you do not get a string assertion offered on an `int`.

Lifecycle hooks exist at every level (test, class, assembly, test session and discovery) as `[Before]`, `[After]`, `[BeforeEvery]` and `[AfterEvery]`. Shared fixtures use `[ClassDataSource<T>]`, with `Shared` set to `PerClass`, `PerAssembly`, `PerTestSession` or a key. That is how this repo starts one `WebApplicationFactory` for the whole run, via [TUnit.AspNetCore](https://tunit.dev/docs/examples/aspnet.md)'s `TestWebApplicationFactory`. There are also first-party packages for [Aspire](https://tunit.dev/docs/examples/aspire.md) (`TUnit.Aspire`, first published on 15 February 2026, [150,481 downloads](https://www.nuget.org/packages/TUnit.Aspire) as of 12 November 2026) and [Playwright](https://tunit.dev/docs/examples/playwright.md). The Aspire fixture starts an AppHost once for the session, which is the sort of thing a platform team actually wants.

`[DependsOn]` lets one test wait for another without turning off parallelism for everything else. `[Retry]` and `[Timeout]` are first-party attributes, at method, class or assembly level.

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

`[MemberData]` becomes `[MethodDataSource]`, and the data method can return tuples instead of `object[]`. TUnit also has `[Matrix]`, which builds every combination of per-parameter values. xUnit has no built-in equivalent, so the xUnit project in this repo lists the same six airport-pair cases as `[InlineData]` rows.

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

For a two-step booking workflow, TUnit can keep the steps as separate tests and still order them, with `[DependsOn(nameof(Step1_Schedule_And_Book))]` on the second test. xUnit has no equivalent, so the xUnit project puts both steps in a single test. Isolated tests are the better default anyway, as TUnit's own docs say; `[DependsOn]` matters when a step is expensive and you really do want it shared.

A short attribute map, taken from TUnit's [migration guide](https://tunit.dev/docs/migration/xunit.md):

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

A sketch, not a full matrix. NUnit and MSTest notes come from TUnit's [framework differences](https://tunit.dev/docs/comparison/framework-differences.md) page and Microsoft's [testing platform overview](https://learn.microsoft.com/en-us/dotnet/core/testing/microsoft-testing-platform-intro).

| | TUnit | xUnit v3 | NUnit | MSTest |
| --- | --- | --- | --- | --- |
| Test method | `[Test]` | `[Fact]` / `[Theory]` | `[Test]` | `[TestMethod]` |
| Discovery | Source generators | Reflection, with AOT work in v3 | Reflection | Reflection |
| Default runner | Microsoft.Testing.Platform only | MTP (default in 4.x) and VSTest | VSTest, plus an MTP runner | VSTest, plus an MTP runner |
| Parallel by default | Yes, every test | Collections in parallel; tests in a collection are sequential | Opt-in | Opt-in |
| Native AOT | First-class, through source generation | TUnit's November 2026 benchmarks include an xUnit v3 AOT column, so support exists | Not a first-class story | Not a first-class story |
| Async assertions | Built in | Classic `Assert`, mostly synchronous | Constraint model | Classic `Assert` |
| Shared fixtures | `ClassDataSource` and a `Shared` mode | `IClassFixture`, `ICollectionFixture` | `OneTimeSetUp`, or a fixture class | `ClassInitialize` |
| Session hooks | `[Before(TestSession)]` / `[After(TestSession)]` | No direct equivalent | `SetUpFixture` is assembly-scoped | `AssemblyInitialize` |
| Test dependencies | `[DependsOn]` | None | `[Order]`, which usually means less parallelism | None |
| Retry / timeout | `[Retry]`, `[Timeout]`, at any level | Timeout on `[Fact]`; no first-party retry | `[Retry]`, `[Timeout]` | Present in recent versions |
| Matrix data | `[Matrix]` | Manual rows or a third-party package | `[Values]` / `[TestCase]` | `[DataRow]` |
| Aspire / Playwright | `TUnit.Aspire`, `TUnit.Playwright` | Community or vendor packages | Playwright has an NUnit package | Playwright has an MSTest package |

NUnit's default of one instance per class, with state leaking between tests, is a well known trap. TUnit, like xUnit, creates a new instance for every test. NUnit's constraint assertions inspired TUnit's fluent style, but TUnit pushes the type checking to the compiler. MSTest remains a solid choice if your team is already on it. Neither is the focus of this repo.

## When you would stay on xUnit

xUnit is the default in almost every .NET tutorial, template and sample. The ecosystem around it (analysers, IDE runners, coverage tools, company standards) is mature. If your team already writes fluent xUnit and your CI is quiet, a migration is a cost with no obvious payoff. TUnit's IDE experience has come on, and Microsoft.Testing.Platform is supported in recent Visual Studio, VS Code and Rider builds, but you should try your actual workflow before you commit. Some older VSTest-only tools will not work, and Coverlet is [incompatible with TUnit](https://tunit.dev/docs/getting-started/installation.md) (TUnit ships Microsoft's coverage extension instead). Familiarity matters as much as features: a framework your colleagues can read on a Friday afternoon is worth more than a slightly nicer assertion syntax. xUnit v3 has also closed some of the gap (MTP by default, a timeout on facts, even an AOT path), so you can take the new runner without changing framework.

## If you do migrate

TUnit ships [Roslyn analysers and a code fixer](https://tunit.dev/docs/migration/xunit.md) (`TUXU0001`) that rewrite most of a typical suite: `[Fact]` and `[Theory]` to `[Test]`, `[InlineData]` to `[Arguments]`, `[MemberData]` to `[MethodDataSource]`, and the common assertions to the fluent form. Add TUnit alongside xUnit, turn off TUnit's implicit usings so the two `Assert` types do not clash, run `dotnet format analyzers --severity info --diagnostics TUXU0001`, then finish by hand. The fixer will not convert fixtures, `IAsyncLifetime` or `ITestOutputHelper`, and data methods should return tuples rather than `object[]`. Then remove the xUnit packages and confirm with `dotnet run -- --list-tests`. Migrate a single project first, keep it green, and only then decide whether the rest follows.

## A note on speed

TUnit [publishes weekly benchmarks](https://tunit.dev/docs/benchmarks) against xUnit v3, NUnit and MSTest, including Native AOT runs. Those are the numbers to look at if performance is your reason for switching. This repo is not that. On 12 November 2026, a Release `dotnet test` run on the machine that built it reported 2.083 seconds for 33 TUnit tests and 2.287 seconds for 32 xUnit tests, including starting an in-process test server. A suite this small proves almost nothing about speed.

## Try it

The code lives in [tonyjoanes/tonys-tunit](https://github.com/tonyjoanes/tonys-tunit). `dotnet test TonysTunit.slnx` runs both projects. If you already have an xUnit suite, the more useful experiment is to take one of your own projects through the migration guide and see how it feels in your IDE and CI.
