using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TonysTunit.Flights.XunitTests.Support;

/// <summary>
/// Shared across every test class in <see cref="FlightsApiCollection"/>.
/// This is the closest xUnit equivalent of TUnit's
/// <c>ClassDataSource</c> with <c>Shared = SharedType.PerTestSession</c>.
/// </summary>
public sealed class FlightsWebFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Testing:DbName"] = "Flights-Xunit-Session"
            });
        });
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

[CollectionDefinition(nameof(FlightsApiCollection))]
public sealed class FlightsApiCollection : ICollectionFixture<FlightsWebFactory>;
