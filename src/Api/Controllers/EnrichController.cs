using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Http.Timeouts;
using Microsoft.AspNetCore.Mvc;
using Api.Extensions;

namespace Api.Controllers;

/// <summary>
/// Provides endpoints for trade data enrichment operations.
/// </summary>
[ApiController]
[Route("api/v1")]
public sealed class EnrichController : ControllerBase
{
    private readonly ILogger<EnrichController> _logger;
    private readonly ITradeEnrichmentService _enrichmentService;

    /// <summary>
    /// Initializes a new instance of the <see cref="EnrichController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="enrichmentService">The trade enrichment service.</param>
    public EnrichController(
        ILogger<EnrichController> logger,
        ITradeEnrichmentService enrichmentService)
    {
        _logger = logger;
        _enrichmentService = enrichmentService;
    }

    /// <summary>
    /// Enriches trade data by mapping product IDs to product names.
    /// </summary>
    /// <param name="trades">The trade input data from CSV.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Enriched trade data with product names and summary headers.</returns>
    /// <response code="200">Returns the enriched trade data with summary headers.</response>
    /// <response code="400">If the input is null or empty.</response>
    /// <response code="408">If the request is cancelled or times out.</response>
    /// <response code="500">If an internal error occurs during processing.</response>
    /// <remarks>
    /// Request size limit is configured via Kestrel in appsettings.json (EnrichmentEndpoint:MaxRequestSizeBytes).
    /// Request timeout is configured via appsettings.json (EnrichmentEndpoint:TimeoutSeconds).
    /// </remarks>
    [HttpPost("enrich")]
    [Consumes("text/csv")]
    [Produces("text/csv")]
    [RequestTimeout("EnrichmentPolicy")]
    public async Task<IActionResult> Enrich(
        [FromBody] IReadOnlyList<TradeInputDto> trades,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (trades is null)
            {
                _logger.LogWarning("Enrich endpoint called with null input");
                return BadRequest("Input cannot be null");
            }

            if (trades.Count == 0)
            {
                _logger.LogWarning("Enrich endpoint called with empty input");
                return BadRequest("Input cannot be empty");
            }

            _logger.LogInformation("Processing {Count} trades for enrichment", trades.Count);

            // Call enrichment service on background thread (CPU-bound work)
            // This prevents blocking the request thread for large datasets
            (IEnumerable<EnrichedTradeOutputDto> enrichedTrades, EnrichmentSummary summary) =
                await Task.Run(() => _enrichmentService.EnrichTrades(trades), cancellationToken);

            // Add summary headers to response
            Response.AddEnrichmentSummaryHeaders(summary);

            _logger.LogInformation(
                "Enrichment completed: {Total} processed, {Enriched} enriched, {Discarded} discarded, {Missing} missing products",
                summary.TotalRowsProcessed,
                summary.RowsSuccessfullyEnriched,
                summary.RowsDiscardedDueToValidation,
                summary.RowsWithMissingProducts);

            // Return 200 OK even if some rows were discarded or had missing products
            // The headers provide the summary information
            return Ok(enrichedTrades);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Enrichment request was cancelled");
            return StatusCode(StatusCodes.Status408RequestTimeout, "Request was cancelled or timed out");
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation during enrichment");
            return BadRequest($"Invalid operation: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during enrichment");
            // Don't include exception details in response to avoid information leakage
            return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred while processing the request");
        }
    }
}
