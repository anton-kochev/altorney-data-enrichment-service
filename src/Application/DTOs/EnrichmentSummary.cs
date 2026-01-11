namespace Application.DTOs;

/// <summary>
/// Represents summary statistics for trade enrichment processing.
/// </summary>
public sealed record EnrichmentSummary
{
    /// <summary>
    /// Gets the total number of rows processed.
    /// </summary>
    public int TotalRowsProcessed { get; init; }

    /// <summary>
    /// Gets the number of rows successfully enriched.
    /// </summary>
    public int RowsSuccessfullyEnriched { get; init; }

    /// <summary>
    /// Gets the number of rows enriched with missing product placeholder.
    /// </summary>
    public int RowsWithMissingProducts { get; init; }

    /// <summary>
    /// Gets the number of rows discarded due to validation failures.
    /// </summary>
    public int RowsDiscardedDueToValidation { get; init; }

    /// <summary>
    /// Gets the set of unique missing product IDs encountered.
    /// </summary>
    public IReadOnlySet<int> MissingProductIds { get; init; } = new HashSet<int>();
}
