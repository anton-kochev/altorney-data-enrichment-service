using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// Represents an input trade with product identifier before enrichment.
/// </summary>
public sealed record Trade
{
    /// <summary>
    /// Gets the trade date.
    /// </summary>
    public TradeDate Date { get; }

    /// <summary>
    /// Gets the product identifier.
    /// </summary>
    public ProductIdentifier ProductId { get; }

    /// <summary>
    /// Gets the currency code.
    /// </summary>
    public Currency Currency { get; }

    /// <summary>
    /// Gets the trade price.
    /// </summary>
    public Price Price { get; }

    private Trade(TradeDate date, ProductIdentifier productId, Currency currency, Price price)
    {
        Date = date;
        ProductId = productId;
        Currency = currency;
        Price = price;
    }

    /// <summary>
    /// Creates a new Trade instance with the specified values.
    /// </summary>
    /// <param name="date">The trade date.</param>
    /// <param name="productId">The product identifier.</param>
    /// <param name="currency">The currency code.</param>
    /// <param name="price">The trade price.</param>
    /// <returns>A new Trade instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when any argument is null.</exception>
    public static Trade Create(TradeDate date, ProductIdentifier productId, Currency currency, Price price)
    {
        ArgumentNullException.ThrowIfNull(date);
        ArgumentNullException.ThrowIfNull(productId);
        ArgumentNullException.ThrowIfNull(currency);
        ArgumentNullException.ThrowIfNull(price);

        return new Trade(date, productId, currency, price);
    }
}
