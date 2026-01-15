using System.Collections.Frozen;
using Application.Services;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public sealed partial class CsvProductLookupService : IProductRepository
{
    private readonly ILogger<CsvProductLookupService> _logger;
    private volatile FrozenDictionary<int, string> _products = FrozenDictionary<int, string>.Empty;

    public CsvProductLookupService(ILogger<CsvProductLookupService> logger)
    {
        _logger = logger;
    }

    public int Count => _products.Count;

    public bool IsLoaded => _products.Count > 0;

    public string? GetProductName(int productId) => _products.GetValueOrDefault(productId);

    public bool TryGetProductName(int productId, out string? productName) => _products.TryGetValue(productId, out productName);

    internal void LoadProducts(Dictionary<int, string> products)
    {
        ArgumentNullException.ThrowIfNull(products);
        _products = products.ToFrozenDictionary();
        LogProductsLoaded(_products.Count);
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Loaded {Count} products into lookup service")]
    private partial void LogProductsLoaded(int count);
}
