namespace Domain.Exceptions;

/// <summary>
/// Exception thrown when a price value is invalid.
/// </summary>
public sealed class InvalidPriceException : DomainException
{
    public decimal? InvalidPrice { get; }

    public InvalidPriceException(string message) : base(message)
    {
    }

    public InvalidPriceException(string message, decimal invalidPrice) : base(message)
    {
        InvalidPrice = invalidPrice;
    }

    public InvalidPriceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
