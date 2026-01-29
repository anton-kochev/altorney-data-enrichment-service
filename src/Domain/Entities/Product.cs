using Domain.ValueObjects;

namespace Domain.Entities;

/// <summary>
/// Represents a product in the reference data catalog.
/// </summary>
public sealed class Product
{
    /// <summary>
    /// Gets the unique identifier for this product.
    /// </summary>
    public required ProductIdentifier ProductId { get; init; }

    /// <summary>
    /// Gets the display name of the product.
    /// </summary>
    public required string ProductName { get; init; }
}
