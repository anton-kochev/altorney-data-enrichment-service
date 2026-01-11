namespace Application.Services;

/// <summary>
/// Provides product lookup operations.
/// </summary>
public interface IProductLookupService
{
    string? GetProductName(int productId);
    bool TryGetProductName(int productId, out string? productName);
    int Count { get; }
    bool IsLoaded { get; }
}
