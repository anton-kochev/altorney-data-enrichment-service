namespace Domain.Exceptions;

/// <summary>
/// Exception thrown when a trade date is invalid.
/// </summary>
public sealed class InvalidTradeDateException : DomainException
{
    public string? InvalidDate { get; }

    public InvalidTradeDateException(string message) : base(message)
    {
    }

    public InvalidTradeDateException(string message, string invalidDate) : base(message)
    {
        InvalidDate = invalidDate;
    }

    public InvalidTradeDateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
