using Application.Configuration;
using Microsoft.Extensions.Options;

namespace Infrastructure.Configuration;

public sealed class ProductDataOptionsValidator : IValidateOptions<ProductDataOptions>
{
    public ValidateOptionsResult Validate(string? name, ProductDataOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.FilePath))
        {
            return ValidateOptionsResult.Fail("ProductData:FilePath cannot be null or empty");
        }

        return ValidateOptionsResult.Success;
    }
}
