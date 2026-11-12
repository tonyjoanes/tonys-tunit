using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using TUnit.AspNetCore;
using TUnit.Core.Interfaces;

namespace TonysTunit.Flights.TUnitTests.Support;

/// <summary>
/// One in-process test server for the whole TUnit session. Seeded flights
/// have plenty of seats, and tests that mutate state use their own flights
/// so they can still run in parallel.
/// </summary>
public sealed class FlightsWebFactory : TestWebApplicationFactory<Program>, IAsyncInitializer
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:DbName"] = "Flights-TUnit-Session"
            });
        });
    }

    public Task InitializeAsync()
    {
        // Force the host to start once, before the first test hits it.
        _ = Server;
        return Task.CompletedTask;
    }

    public HttpClient CreateJsonClient()
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });
        client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
        return client;
    }
}
