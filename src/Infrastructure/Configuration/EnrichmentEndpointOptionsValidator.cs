using Application.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Configuration;

public sealed class EnrichmentEndpointOptionsValidator : IValidateOptions<EnrichmentEndpointOptions>
{
    public ValidateOptionsResult Validate(string? name, EnrichmentEndpointOptions options)
    {
        if (options.MaxRequestSizeBytes <= 0)
        {
            return ValidateOptionsResult.Fail(
                "EnrichmentEndpoint:MaxRequestSizeBytes must be greater than 0");
        }

        if (options.TimeoutSeconds <= 0)
        {
            return ValidateOptionsResult.Fail(
                "EnrichmentEndpoint:TimeoutSeconds must be greater than 0");
        }

        return ValidateOptionsResult.Success;
    }
}
