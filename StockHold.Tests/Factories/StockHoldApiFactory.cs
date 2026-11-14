
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace StockHold.Tests.Factories;

public sealed class StockHoldApiFactory
    : WebApplicationFactory<Program>
{
    private readonly string _connectionString;

    public StockHoldApiFactory(string connectionString)
    {
        _connectionString = connectionString;
    }

    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            var settings = new Dictionary<string, string?>
            {
                {
                    "ConnectionStrings:StockHoldConnection",
                    _connectionString
                }
            };

            configuration.AddInMemoryCollection(settings);
        });
    }
}
