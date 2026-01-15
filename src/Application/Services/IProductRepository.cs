namespace Application.Services;

/// <summary>
/// Repository for product data access operations.
/// </summary>
public interface IProductRepository
{
    string? GetProductName(int productId);
    bool TryGetProductName(int productId, out string? productName);
    int Count { get; }
    bool IsLoaded { get; }
}
