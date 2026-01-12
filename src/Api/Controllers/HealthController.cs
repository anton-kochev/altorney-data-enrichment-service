using Application.DTOs;
using Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

/// <summary>
/// Provides health check endpoint for service monitoring.
/// </summary>
[ApiController]
public sealed partial class HealthController : ControllerBase
{
    private readonly ILogger<HealthController> _logger;
    private readonly IProductLookupService _productLookupService;

    /// <summary>
    /// Initializes a new instance of the <see cref="HealthController"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="productLookupService">The product lookup service.</param>
    public HealthController(
        ILogger<HealthController> logger,
        IProductLookupService productLookupService)
    {
        _logger = logger;
        _productLookupService = productLookupService;
    }

    /// <summary>
    /// Returns the health status of the service and product data availability.
    /// </summary>
    /// <returns>Health check response with status, product data loaded flag, product count, and timestamp.</returns>
    /// <response code="200">Service is healthy and product data is loaded.</response>
    /// <response code="503">Service is unhealthy or product data is not loaded.</response>
    [HttpGet("/health")]
    [Produces("application/json")]
    public IActionResult GetHealth()
    {
        var isLoaded = _productLookupService.IsLoaded;
        var productCount = _productLookupService.Count;
        var status = isLoaded ? "Healthy" : "Unhealthy";
        var timestamp = DateTime.UtcNow;

        var response = new HealthCheckResponseDto
        {
            Status = status,
            ProductDataLoaded = isLoaded,
            ProductCount = productCount,
            Timestamp = timestamp
        };

        if (isLoaded)
        {
            LogHealthCheckHealthy(status, isLoaded, productCount);
            return Ok(response);
        }
        else
        {
            LogHealthCheckUnhealthy(status, isLoaded, productCount);
            return StatusCode(StatusCodes.Status503ServiceUnavailable, response);
        }
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Information,
        Message = "Health check executed: Status={Status}, ProductDataLoaded={ProductDataLoaded}, ProductCount={ProductCount}")]
    private partial void LogHealthCheckHealthy(string status, bool productDataLoaded, int productCount);

    [LoggerMessage(
        EventId = 2,
        Level = LogLevel.Warning,
        Message = "Health check executed: Status={Status}, ProductDataLoaded={ProductDataLoaded}, ProductCount={ProductCount}")]
    private partial void LogHealthCheckUnhealthy(string status, bool productDataLoaded, int productCount);
}
