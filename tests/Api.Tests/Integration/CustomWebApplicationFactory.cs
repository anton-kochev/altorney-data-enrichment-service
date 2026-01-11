using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace Api.Tests.Integration;

/// <summary>
/// Custom WebApplicationFactory that configures test-specific settings.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((context, config) =>
        {
            // Get the test data directory path
            var testDataPath = Path.Combine(
                AppContext.BaseDirectory,
                "TestData",
                "product.csv");

            // Override product data file path for tests
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ProductData:FilePath"] = testDataPath
            });
        });
    }
}
