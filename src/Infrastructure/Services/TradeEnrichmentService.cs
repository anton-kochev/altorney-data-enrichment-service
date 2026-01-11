using System.Collections.Concurrent;
using System.Globalization;
using Application.DTOs;
using Application.Services;
using Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

/// <summary>
/// Provides trade enrichment operations that map product IDs to product names.
/// </summary>
public sealed partial class TradeEnrichmentService : ITradeEnrichmentService
{
    private const string MissingProductNamePlaceholder = "Missing Product Name";

    private readonly IProductLookupService _productLookupService;
    private readonly ILogger<TradeEnrichmentService> _logger;
    private readonly ConcurrentDictionary<int, byte> _loggedMissingProductIds = new();

    public TradeEnrichmentService(
        IProductLookupService productLookupService,
        ILogger<TradeEnrichmentService> logger)
    {
        ArgumentNullException.ThrowIfNull(productLookupService);
        ArgumentNullException.ThrowIfNull(logger);

        _productLookupService = productLookupService;
        _logger = logger;
    }

    /// <inheritdoc />
    public EnrichedTradeOutputDto? EnrichTrade(TradeInputDto tradeInput)
    {
        ArgumentNullException.ThrowIfNull(tradeInput);

        // Validate and parse input fields
        if (!TryValidateAndParse(tradeInput, out var date, out var productId, out var currency, out var price))
        {
            return null;
        }

        // Look up product name
        string productName;

        if (_productLookupService.TryGetProductName(productId, out var foundName) && foundName is not null)
        {
            productName = foundName;
        }
        else
        {
            productName = MissingProductNamePlaceholder;
            LogMissingProductIfNeeded(productId, date, currency, price);
        }

        return new EnrichedTradeOutputDto
        {
            Date = date,
            ProductName = productName,
            Currency = currency,
            Price = price
        };
    }

    /// <inheritdoc />
    public (IEnumerable<EnrichedTradeOutputDto> EnrichedTrades, EnrichmentSummary Summary) EnrichTrades(
        IEnumerable<TradeInputDto> trades)
    {
        ArgumentNullException.ThrowIfNull(trades);

        var enrichedTrades = new List<EnrichedTradeOutputDto>();
        var missingProductIds = new HashSet<int>();
        int totalRows = 0;
        int rowsEnriched = 0;
        int rowsWithMissingProducts = 0;
        int rowsDiscarded = 0;

        foreach (var trade in trades)
        {
            totalRows++;

            // Validate and parse input fields
            if (!TryValidateAndParse(trade, out var date, out var productId, out var currency, out var price))
            {
                rowsDiscarded++;
                continue;
            }

            // Look up product name
            string productName;
            bool hasMissingProduct = false;

            if (_productLookupService.TryGetProductName(productId, out var foundName) && foundName is not null)
            {
                productName = foundName;
            }
            else
            {
                productName = MissingProductNamePlaceholder;
                hasMissingProduct = true;
                missingProductIds.Add(productId);
                LogMissingProductIfNeeded(productId, date, currency, price);
            }

            enrichedTrades.Add(new EnrichedTradeOutputDto
            {
                Date = date,
                ProductName = productName,
                Currency = currency,
                Price = price
            });

            if (hasMissingProduct)
            {
                rowsWithMissingProducts++;
            }
            else
            {
                rowsEnriched++;
            }
        }

        var summary = new EnrichmentSummary
        {
            TotalRowsProcessed = totalRows,
            RowsSuccessfullyEnriched = rowsEnriched,
            RowsWithMissingProducts = rowsWithMissingProducts,
            RowsDiscardedDueToValidation = rowsDiscarded,
            MissingProductIds = missingProductIds
        };

        return (enrichedTrades, summary);
    }

    private bool TryValidateAndParse(
        TradeInputDto input,
        out string dateStr,
        out int productId,
        out string currency,
        out string price)
    {
        dateStr = string.Empty;
        productId = 0;
        currency = string.Empty;
        price = string.Empty;

        try
        {
            // Validate date using TradeDate value object
            var tradeDate = TradeDate.Create(input.Date);
            dateStr = tradeDate.FormattedValue;

            // Validate and parse productId
            if (!int.TryParse(input.ProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out productId))
            {
                return false;
            }

            // Validate productId is positive using ProductIdentifier
            _ = ProductIdentifier.Create(productId);

            // Validate currency using Currency value object
            var currencyValue = Currency.Create(input.Currency);
            currency = currencyValue.Value;

            // Validate and parse price
            if (!decimal.TryParse(input.Price, NumberStyles.Number, CultureInfo.InvariantCulture, out var priceDecimal))
            {
                return false;
            }

            // Validate price is non-negative using Price value object
            _ = Price.Create(priceDecimal);
            price = input.Price;

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    /// <summary>
    /// Logs a warning for a missing product if not already logged.
    /// </summary>
    /// <param name="productId">The product ID that was not found.</param>
    /// <param name="date">The validated trade date in yyyyMMdd format.</param>
    /// <param name="currency">The validated currency code.</param>
    /// <param name="price">The validated price as a string.</param>
    /// <remarks>
    /// This method expects pre-validated parameters from <see cref="TryValidateAndParse"/>.
    /// Duplicate product IDs are logged only once per service instance.
    /// </remarks>
    private void LogMissingProductIfNeeded(int productId, string date, string currency, string price)
    {
        if (_loggedMissingProductIds.TryAdd(productId, 0))
        {
            LogMissingProduct(productId, date, currency, price);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Warning,
        Message = "Product ID {ProductId} not found in product reference data. Trade: Date={Date}, Currency={Currency}, Price={Price}. Using placeholder.")]
    private partial void LogMissingProduct(int productId, string date, string currency, string price);
}
