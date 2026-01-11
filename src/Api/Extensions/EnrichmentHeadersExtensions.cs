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
        response.Headers.Append("X-Enrichment-Unique-Missing-Product-Ids", summary.MissingProductIds.Count.ToString());
    }
}
