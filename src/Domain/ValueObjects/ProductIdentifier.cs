namespace Domain.ValueObjects;

/// <summary>
/// Represents a product identifier.
/// </summary>
public sealed record ProductIdentifier
{
    /// <summary>
    /// Gets the product ID value.
    /// </summary>
    public int Value { get; }

    private ProductIdentifier(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a ProductIdentifier from a positive integer.
    /// </summary>
    /// <param name="productId">The product ID. Must be greater than 0.</param>
    /// <returns>A new ProductIdentifier instance.</returns>
    /// <exception cref="ArgumentException">Thrown when productId is not a positive integer.</exception>
    public static ProductIdentifier Create(int productId)
    {
        if (productId <= 0)
        {
            throw new ArgumentException("Product ID must be a positive integer (greater than 0).", nameof(productId));
        }

        return new ProductIdentifier(productId);
    }
}
