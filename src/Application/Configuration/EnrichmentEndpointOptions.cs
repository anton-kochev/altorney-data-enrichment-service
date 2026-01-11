namespace Application.Configuration;

/// <summary>
/// Configuration options for the enrichment endpoint.
/// </summary>
public sealed class EnrichmentEndpointOptions
{
    public const string SectionName = "EnrichmentEndpoint";

    /// <summary>
    /// Maximum request body size in bytes. Default: 100MB.
    /// </summary>
    public long MaxRequestSizeBytes { get; init; } = 104_857_600;

    /// <summary>
    /// Request timeout in seconds. Default: 5 minutes.
    /// </summary>
    public int TimeoutSeconds { get; init; } = 300;
}
