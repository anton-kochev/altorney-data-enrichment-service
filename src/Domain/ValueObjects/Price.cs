namespace Domain.ValueObjects;

/// <summary>
/// Represents a price value.
/// </summary>
public sealed record Price
{
    /// <summary>
    /// Gets the price amount.
    /// </summary>
    public decimal Value { get; }

    private Price(decimal value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a Price from a decimal amount.
    /// </summary>
    /// <param name="amount">The price amount. Must be greater than or equal to 0.</param>
    /// <returns>A new Price instance.</returns>
    /// <exception cref="ArgumentException">Thrown when amount is negative.</exception>
    public static Price Create(decimal amount)
    {
        if (amount < 0)
        {
            throw new ArgumentException("Price cannot be negative.", nameof(amount));
        }

        return new Price(amount);
    }
}
