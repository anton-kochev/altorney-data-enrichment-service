namespace Domain.ValueObjects;

/// <summary>
/// Represents a trade date in yyyyMMdd format.
/// </summary>
public sealed record TradeDate
{
    /// <summary>
    /// Gets the date value.
    /// </summary>
    public DateOnly Value { get; }

    /// <summary>
    /// Gets the formatted date string in yyyyMMdd format.
    /// </summary>
    public string FormattedValue { get; }

    private TradeDate(DateOnly value)
    {
        Value = value;
        FormattedValue = value.ToString("yyyyMMdd");
    }

    /// <summary>
    /// Creates a TradeDate from a string in yyyyMMdd format.
    /// </summary>
    /// <param name="dateString">The date string in yyyyMMdd format.</param>
    /// <returns>A new TradeDate instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when dateString is null.</exception>
    /// <exception cref="ArgumentException">Thrown when dateString is empty, whitespace, has invalid format, or represents an invalid date.</exception>
    public static TradeDate Create(string dateString)
    {
        ArgumentNullException.ThrowIfNull(dateString, nameof(dateString));

        if (string.IsNullOrWhiteSpace(dateString))
        {
            throw new ArgumentException("Date string cannot be null or empty.", nameof(dateString));
        }

        // Check exact length (8 characters for yyyyMMdd)
        if (dateString.Length != 8)
        {
            throw new ArgumentException("Date must be in yyyyMMdd format.", nameof(dateString));
        }

        // Validate year portion (first 4 characters) to ensure format correctness
        // Years 1-999 are technically valid dates but indicate wrong format (likely DD/MM/YYYY)
        // Year 0 should be allowed to pass to date validation which will fail appropriately
        if (!int.TryParse(dateString.Substring(0, 4), out var year) || (year > 0 && year < 1000))
        {
            throw new ArgumentException("Date must be in yyyyMMdd format.", nameof(dateString));
        }

        // Try to parse the date using exact format
        if (!DateOnly.TryParseExact(dateString, "yyyyMMdd", null, System.Globalization.DateTimeStyles.None, out var parsedDate))
        {
            // Check if it's a format issue or invalid date
            // If all characters are digits, it's likely an invalid date, otherwise it's a format issue
            if (dateString.All(char.IsDigit))
            {
                throw new ArgumentException("Date string does not represent a valid calendar date.", nameof(dateString));
            }
            else
            {
                throw new ArgumentException("Date must be in yyyyMMdd format.", nameof(dateString));
            }
        }

        return new TradeDate(parsedDate);
    }
}
