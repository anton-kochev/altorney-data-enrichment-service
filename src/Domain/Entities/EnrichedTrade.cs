using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// Represents an enriched trade with product name instead of product identifier.
/// </summary>
public sealed record EnrichedTrade
{
    /// <summary>
    /// Gets the trade date.
    /// </summary>
    public TradeDate Date { get; }

    /// <summary>
    /// Gets the product name.
    /// </summary>
    public string ProductName { get; }

    /// <summary>
    /// Gets the currency code.
    /// </summary>
    public Currency Currency { get; }

    /// <summary>
    /// Gets the trade price.
    /// </summary>
    public Price Price { get; }

    private EnrichedTrade(TradeDate date, string productName, Currency currency, Price price)
    {
        Date = date;
        ProductName = productName;
        Currency = currency;
        Price = price;
    }

    /// <summary>
    /// Creates a new EnrichedTrade instance with the specified values.
    /// </summary>
    /// <param name="date">The trade date.</param>
    /// <param name="productName">The product name.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="price">The trade price.</param>
    /// <returns>A new EnrichedTrade instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static EnrichedTrade Create(TradeDate date, string productName, Currency currency, Price price)
    {
        ArgumentNullException.ThrowIfNull(date);
        ArgumentNullException.ThrowIfNull(productName);
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentNullException.ThrowIfNull(price);

        return new EnrichedTrade(date, productName, currency, price);
    }
}
