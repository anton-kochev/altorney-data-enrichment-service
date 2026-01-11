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

        // Look up a product name
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
        }
        catch (ArgumentException ex)
        {
            // Log date validation errors with full trade context
            LogInvalidDateFormat(input.Date, input.ProductId, input.Currency, input.Price, ex.Message);
            return false;
        }

        try
        {
            // Validate and parse productId
            if (!int.TryParse(input.ProductId, NumberStyles.Integer, CultureInfo.InvariantCulture, out productId))
            {
                return false;
            }

            // Validate productId is positive using ProductIdentifier
            _ = ProductIdentifier.Create(productId);

            // Validate currency using the Currency value object
            var currencyValue = Currency.Create(input.Currency);
            currency = currencyValue.Value;

            // Validate and parse price (trim first to reduce allocations)
            string trimmedPrice = input.Price.Trim();
            if (!decimal.TryParse(trimmedPrice, NumberStyles.Number, CultureInfo.InvariantCulture, out var priceDecimal))
            {
                return false;
            }

            // Validate price is non-negative using the Price value object
            _ = Price.Create(priceDecimal);
            price = trimmedPrice;

            return true;
        }
        catch (ArgumentException)
        {
            // Other validation failures (productId, currency, price) - no logging
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

    /// <summary>
    /// Logs an error for an invalid date format in a trade row.
    /// </summary>
    /// <param name="date">The invalid date string.</param>
    /// <param name="productId">The product ID from the trade row.</param>
    /// <param name="currency">The currency from the trade row.</param>
    /// <param name="price">The price from the trade row.</param>
    /// <param name="reason">The reason the date validation failed.</param>
    /// <remarks>
    /// Unlike missing products (which are logged once per unique ID), each invalid date
    /// is logged because invalid dates represent data quality errors requiring an audit trail.
    /// </remarks>
    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Invalid date format in trade row. Date='{Date}', ProductId={ProductId}, Currency={Currency}, Price={Price}. Reason: {Reason}. Row discarded.")]
    private partial void LogInvalidDateFormat(string date, string productId, string currency, string price, string reason);
}
