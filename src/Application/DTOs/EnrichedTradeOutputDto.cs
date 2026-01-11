namespace Application.DTOs;

/// <summary>
/// Represents enriched trade output data with product name instead of product ID.
/// </summary>
public sealed record EnrichedTradeOutputDto
{
    /// <summary>
    /// Gets the trade date in yyyyMMdd format.
    /// </summary>
    public required string Date { get; init; }

    /// <summary>
    /// Gets the product name (enriched from product ID lookup).
    /// </summary>
    public required string ProductName { get; init; }

    /// <summary>
    /// Gets the currency code.
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Gets the trade price.
    /// </summary>
    public required string Price { get; init; }
}
