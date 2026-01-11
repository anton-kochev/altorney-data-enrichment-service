namespace Domain.Exceptions;

/// <summary>
/// Exception thrown when a product identifier is invalid.
/// </summary>
public sealed class InvalidProductIdentifierException : DomainException
{
    public int? InvalidProductId { get; }

    public InvalidProductIdentifierException(string message) : base(message)
    {
    }

    public InvalidProductIdentifierException(string message, int invalidProductId) : base(message)
    {
        InvalidProductId = invalidProductId;
    }

    public InvalidProductIdentifierException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
