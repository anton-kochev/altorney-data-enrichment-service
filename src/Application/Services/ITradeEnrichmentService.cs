using Application.DTOs;

namespace Application.Services;

/// <summary>
/// Provides trade enrichment operations that map product IDs to product names.
/// </summary>
public interface ITradeEnrichmentService
{
    /// <summary>
    /// Enriches a single trade by looking up the product name for the given product ID.
    /// </summary>
    /// <param name="tradeInput">The trade input data to enrich.</param>
    /// <returns>
    /// An enriched trade output with product name, or null if the trade failed validation.
    /// </returns>
    EnrichedTradeOutputDto? EnrichTrade(TradeInputDto tradeInput);

    /// <summary>
    /// Enriches multiple trades and returns summary statistics.
    /// </summary>
    /// <param name="trades">The trade input data to enrich.</param>
    /// <returns>
    /// A tuple containing the enriched trades and summary statistics.
    /// Invalid trades are excluded from the enriched trades collection.
    /// </returns>
    (IEnumerable<EnrichedTradeOutputDto> EnrichedTrades, EnrichmentSummary Summary) EnrichTrades(
        IEnumerable<TradeInputDto> trades);
}
