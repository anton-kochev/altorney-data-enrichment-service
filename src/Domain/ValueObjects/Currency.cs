namespace Domain.ValueObjects;

/// <summary>
/// Represents a currency code.
/// </summary>
public sealed record Currency
{
    /// <summary>
    /// Gets the currency code value.
    /// </summary>
    public string Value { get; }

    private Currency(string value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a Currency from a currency code string.
    /// </summary>
    /// <param name="currencyCode">The currency code. Cannot be null, empty, or whitespace.</param>
    /// <returns>A new Currency instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when currencyCode is null.</exception>
    /// <exception cref="ArgumentException">Thrown when currencyCode is empty or whitespace.</exception>
    public static Currency Create(string currencyCode)
    {
        ArgumentNullException.ThrowIfNull(currencyCode, nameof(currencyCode));

        var trimmedCode = currencyCode.Trim();

        if (string.IsNullOrEmpty(trimmedCode))
        {
            throw new ArgumentException("Currency code cannot be null or empty.", nameof(currencyCode));
        }

        return new Currency(trimmedCode);
    }
}
