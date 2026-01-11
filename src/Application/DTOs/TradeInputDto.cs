namespace Application.DTOs;

/// <summary>
/// Represents raw trade input data with string fields from CSV.
/// </summary>
public sealed record TradeInputDto
{
    /// <summary>
    /// Gets the trade date in raw string format (expected: yyyyMMdd).
    /// </summary>
    public required string Date { get; init; }

    /// <summary>
    /// Gets the product identifier in raw string format (expected: positive integer).
    /// </summary>
    public required string ProductId { get; init; }

    /// <summary>
    /// Gets the currency code in raw string format.
    /// </summary>
    public required string Currency { get; init; }

    /// <summary>
    /// Gets the trade price in raw string format (expected: decimal).
    /// </summary>
    public required string Price { get; init; }
}
