namespace Application.DTOs;

/// <summary>
/// Response DTO for the health check endpoint.
/// </summary>
public sealed record HealthCheckResponseDto
{
    /// <summary>
    /// Health status: "Healthy" or "Unhealthy".
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Indicates whether product reference data is loaded.
    /// </summary>
    public required bool ProductDataLoaded { get; init; }

    /// <summary>
    /// Number of products loaded in the lookup service.
    /// </summary>
    public required int ProductCount { get; init; }

    /// <summary>
    /// UTC timestamp when health check was performed.
    /// </summary>
    public required DateTime Timestamp { get; init; }
}
