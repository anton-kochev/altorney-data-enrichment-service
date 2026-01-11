using Application.Configuration;
using Application.Services;
using Infrastructure.Configuration;
using Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Extensions;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<ProductDataOptions>, ProductDataOptionsValidator>();
        services
            .AddOptions<ProductDataOptions>()
            .Bind(configuration.GetSection(ProductDataOptions.SectionName))
            .ValidateOnStart();

        services.AddSingleton<CsvProductLookupService>();
        services.AddSingleton<IProductLookupService>(sp =>
            sp.GetRequiredService<CsvProductLookupService>());

        services.AddHostedService<ProductDataLoader>();

        return services;
    }
}
