using System.Collections.Concurrent;
using Application.DTOs;
using Application.Mappers;
using Domain.Constants;
using Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Application.Services;

/// <summary>
/// Provides trade enrichment operations that map product IDs to product names.
/// </summary>
public sealed partial class TradeEnrichmentService : ITradeEnrichmentService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<TradeEnrichmentService> _logger;
    private readonly ConcurrentDictionary<int, byte> _loggedMissingProductIds = new();

    public TradeEnrichmentService(
        IProductRepository productRepository,
        ILogger<TradeEnrichmentService> logger)
    {
        ArgumentNullException.ThrowIfNull(productRepository);
        ArgumentNullException.ThrowIfNull(logger);

        _productRepository = productRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public EnrichedTradeOutputDto? EnrichTrade(TradeInputDto tradeInput)
    {
        ArgumentNullException.ThrowIfNull(tradeInput);

        // Step 1: Map DTO to Trade domain entity (includes validation)
        var mappingResult = TradeMapper.TryMapToTrade(tradeInput);
        if (!mappingResult.IsSuccess)
        {
            LogValidationFailure(mappingResult.Failure!);
            return null;
        }

        Trade trade = mappingResult.Trade!;

        // Step 2: Look up product name
        string productName;
        if (_productRepository.TryGetProductName(trade.ProductId.Value, out string? foundName) && foundName is not null)
        {
            productName = foundName;
        }
        else
        {
            productName = TradeConstants.MissingProductNamePlaceholder;
            LogMissingProductIfNeeded(
                trade.ProductId.Value,
                trade.Date.FormattedValue,
                trade.Currency.Value,
                mappingResult.TrimmedPrice);
        }

        // Step 3: Create EnrichedTrade domain entity
        var enrichedTrade = EnrichedTrade.Create(trade.Date, productName, trade.Currency, trade.Price);

        // Step 4: Map to output DTO
        return EnrichedTradeMapper.ToDto(enrichedTrade, mappingResult.TrimmedPrice);
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

        foreach (TradeInputDto tradeInput in trades)
        {
            totalRows++;

            // Step 1: Map DTO to Trade domain entity (includes validation)
            var mappingResult = TradeMapper.TryMapToTrade(tradeInput);
            if (!mappingResult.IsSuccess)
            {
                LogValidationFailure(mappingResult.Failure!);
                rowsDiscarded++;
                continue;
            }

            Trade trade = mappingResult.Trade!;

            // Step 2: Look up product name
            string productName;
            bool hasMissingProduct = false;

            if (_productRepository.TryGetProductName(trade.ProductId.Value, out string? foundName) && foundName is not null)
            {
                productName = foundName;
            }
            else
            {
                productName = TradeConstants.MissingProductNamePlaceholder;
                hasMissingProduct = true;
                missingProductIds.Add(trade.ProductId.Value);
                LogMissingProductIfNeeded(
                    trade.ProductId.Value,
                    trade.Date.FormattedValue,
                    trade.Currency.Value,
                    mappingResult.TrimmedPrice);
            }

            // Step 3: Create EnrichedTrade domain entity
            var enrichedTrade = EnrichedTrade.Create(trade.Date, productName, trade.Currency, trade.Price);

            // Step 4: Map to output DTO
            enrichedTrades.Add(EnrichedTradeMapper.ToDto(enrichedTrade, mappingResult.TrimmedPrice));

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

    /// <summary>
    /// Logs validation failures based on the failure type.
    /// </summary>
    private void LogValidationFailure(TradeMapper.ValidationFailure failure)
    {
        switch (failure.Type)
        {
            case TradeMapper.ValidationFailureType.MissingFields:
                LogMissingFields(
                    string.Join(", ", failure.MissingFields!),
                    failure.RawInput.Date,
                    failure.RawInput.ProductId,
                    failure.RawInput.Currency,
                    failure.RawInput.Price);
                break;

            case TradeMapper.ValidationFailureType.InvalidDateFormat:
                LogInvalidDateFormat(
                    failure.RawInput.Date,
                    failure.RawInput.ProductId,
                    failure.RawInput.Currency,
                    failure.RawInput.Price,
                    failure.InvalidDateReason!);
                break;

            // Other validation failures (productId, currency, price) - no logging per original design
            default:
                break;
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

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Error,
        Message = "Trade record discarded due to missing required fields: [{MissingFields}]. Raw input: Date='{RawDate}', ProductId='{RawProductId}', Currency='{RawCurrency}', Price='{RawPrice}'")]
    private partial void LogMissingFields(
        string missingFields,
        string rawDate,
        string rawProductId,
        string rawCurrency,
        string rawPrice);
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
        EventId = 3,
        Level = LogLevel.Error,
        Message = "Invalid date format in trade row. Date='{Date}', ProductId={ProductId}, Currency={Currency}, Price={Price}. Reason: {Reason}. Row discarded.")]
    private partial void LogInvalidDateFormat(string date, string productId, string currency, string price, string reason);
}
