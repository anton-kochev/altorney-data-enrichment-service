namespace Domain.Exceptions;

/// <summary>
/// Exception thrown when a currency code is invalid.
/// </summary>
public sealed class InvalidCurrencyException : DomainException
{
    public string? InvalidCurrencyCode { get; }

    public InvalidCurrencyException(string message) : base(message)
    {
    }

    public InvalidCurrencyException(string message, string? invalidCurrencyCode) : base(message)
    {
        InvalidCurrencyCode = invalidCurrencyCode;
    }

    public InvalidCurrencyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
