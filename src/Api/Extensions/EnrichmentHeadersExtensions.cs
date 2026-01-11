using Application.DTOs;

namespace Api.Extensions;

/// <summary>
/// Provides extension methods for adding enrichment summary headers to HTTP responses.
/// </summary>
public static class EnrichmentHeadersExtensions
{
    /// <summary>
    /// Adds enrichment summary information as HTTP response headers.
    /// </summary>
    /// <param name="response">The HTTP response to add headers to.</param>
    /// <param name="summary">The enrichment summary containing statistics.</param>
    public static void AddEnrichmentSummaryHeaders(this HttpResponse response, EnrichmentSummary summary)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentNullException.ThrowIfNull(summary);

        response.Headers.Append("X-Enrichment-Total-Rows", summary.TotalRowsProcessed.ToString());
        response.Headers.Append("X-Enrichment-Enriched-Rows", summary.RowsSuccessfullyEnriched.ToString());
        response.Headers.Append("X-Enrichment-Discarded-Rows", summary.RowsDiscardedDueToValidation.ToString());
        response.Headers.Append("X-Enrichment-Missing-Products", summary.RowsWithMissingProducts.ToString());

        // Sort missing product IDs and join with comma separator
        // Always add the header, even if empty, for consistent response format
        var missingProductIds = summary.MissingProductIds.Count > 0
            ? string.Join(",", summary.MissingProductIds.OrderBy(id => id))
            : string.Empty;

        response.Headers["X-Enrichment-Missing-Product-Ids"] = missingProductIds;
    }
}
