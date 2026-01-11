namespace Application.Configuration;

public sealed class ProductDataOptions
{
    public const string SectionName = "ProductData";
    public required string FilePath { get; init; }
}
