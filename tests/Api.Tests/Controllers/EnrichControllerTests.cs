using Api.Tests.Helpers;
using Application.DTOs;
using Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Api.Tests.Controllers;

/// <summary>
/// Unit tests for EnrichController following TDD approach.
/// These tests define the expected behavior of the CSV enrichment endpoint (POST /api/v1/enrich).
/// </summary>
public class EnrichControllerTests : IDisposable
{
    private readonly FakeLogger<Api.Controllers.EnrichController> _fakeLogger;
    private readonly Mock<ITradeEnrichmentService> _mockEnrichmentService;
    private readonly Api.Controllers.EnrichController _sut;
    private readonly DefaultHttpContext _httpContext;

    public EnrichControllerTests()
    {
        _fakeLogger = new FakeLogger<Api.Controllers.EnrichController>();
        _mockEnrichmentService = new Mock<ITradeEnrichmentService>();
        _sut = new Api.Controllers.EnrichController(_fakeLogger, _mockEnrichmentService.Object);

        // Set up HTTP context for response header testing
        _httpContext = new DefaultHttpContext();
        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = _httpContext
        };
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Happy Path Tests

    [Fact]
    public async Task Enrich_WithValidTrades_ReturnsOkResult()
    {
        // Arrange
        var inputTrades = new[] { TestDataBuilder.CreateValidTradeInput() };
        var enrichedTrades = new[] { TestDataBuilder.CreateEnrichedTradeOutput() };
        var summary = CreateSummary(totalProcessed: 1, enriched: 1);

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        var result = await _sut.Enrich(inputTrades);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task Enrich_WithValidTrades_ReturnsEnrichedTrades()
    {
        // Arrange
        var inputTrades = new[]
        {
            TestDataBuilder.CreateValidTradeInput(date: "20260111", productId: "1", currency: "USD", price: "100.50"),
            TestDataBuilder.CreateValidTradeInput(date: "20260112", productId: "2", currency: "EUR", price: "200.75")
        };

        var enrichedTrades = new[]
        {
            TestDataBuilder.CreateEnrichedTradeOutput(date: "20260111", productName: "Treasury Bills Domestic", currency: "USD", price: "100.50"),
            TestDataBuilder.CreateEnrichedTradeOutput(date: "20260112", productName: "Corporate Bonds", currency: "EUR", price: "200.75")
        };

        var summary = CreateSummary(totalProcessed: 2, enriched: 2);

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        var result = await _sut.Enrich(inputTrades);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTrades = okResult.Value.Should().BeAssignableTo<IEnumerable<EnrichedTradeOutputDto>>().Subject.ToList();

        returnedTrades.Should().HaveCount(2);
        returnedTrades[0].Date.Should().Be("20260111");
        returnedTrades[0].ProductName.Should().Be("Treasury Bills Domestic");
        returnedTrades[0].Currency.Should().Be("USD");
        returnedTrades[0].Price.Should().Be("100.50");

        returnedTrades[1].Date.Should().Be("20260112");
        returnedTrades[1].ProductName.Should().Be("Corporate Bonds");
        returnedTrades[1].Currency.Should().Be("EUR");
        returnedTrades[1].Price.Should().Be("200.75");
    }

    [Fact]
    public async Task Enrich_WithValidTrades_CallsEnrichmentService()
    {
        // Arrange
        var inputTrades = new[]
        {
            TestDataBuilder.CreateValidTradeInput(productId: "1"),
            TestDataBuilder.CreateValidTradeInput(productId: "2"),
            TestDataBuilder.CreateValidTradeInput(productId: "3")
        };

        var enrichedTrades = new[]
        {
            TestDataBuilder.CreateEnrichedTradeOutput(),
            TestDataBuilder.CreateEnrichedTradeOutput(),
            TestDataBuilder.CreateEnrichedTradeOutput()
        };

        var summary = CreateSummary(totalProcessed: 3, enriched: 3);

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        await _sut.Enrich(inputTrades);

        // Assert
        _mockEnrichmentService.Verify(
            s => s.EnrichTrades(It.Is<IEnumerable<TradeInputDto>>(
                trades => trades.Count() == 3 &&
                         trades.ElementAt(0).ProductId == "1" &&
                         trades.ElementAt(1).ProductId == "2" &&
                         trades.ElementAt(2).ProductId == "3")),
            Times.Once);
    }

    [Fact]
    public async Task Enrich_WithValidTrades_AddsEnrichmentSummaryHeaders()
    {
        // Arrange
        var inputTrades = new[] { TestDataBuilder.CreateValidTradeInput() };
        var enrichedTrades = new[] { TestDataBuilder.CreateEnrichedTradeOutput() };
        var summary = CreateSummary(
            totalProcessed: 100,
            enriched: 95,
            discarded: 3,
            missingProducts: 2,
            missingProductIds: new HashSet<int> { 999, 888 });

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        await _sut.Enrich(inputTrades);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Total-Rows")
            .WhoseValue.ToString().Should().Be("100");
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Enriched-Rows")
            .WhoseValue.ToString().Should().Be("95");
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Discarded-Rows")
            .WhoseValue.ToString().Should().Be("3");
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Missing-Products")
            .WhoseValue.ToString().Should().Be("2");
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Missing-Product-Ids")
            .WhoseValue.ToString().Should().Be("888,999");
    }

    #endregion

    #region Partial Success Tests

    [Fact]
    public async Task Enrich_WithSomeInvalidRows_ReturnsOkResult()
    {
        // Arrange
        var inputTrades = new[]
        {
            TestDataBuilder.CreateValidTradeInput(productId: "1"),
            TestDataBuilder.CreateValidTradeInput(productId: "2"),
            TestDataBuilder.CreateValidTradeInput(productId: "3")
        };

        // Only 2 trades enriched (1 failed validation)
        var enrichedTrades = new[]
        {
            TestDataBuilder.CreateEnrichedTradeOutput(),
            TestDataBuilder.CreateEnrichedTradeOutput()
        };

        var summary = CreateSummary(totalProcessed: 3, enriched: 2, discarded: 1);

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        var result = await _sut.Enrich(inputTrades);

        // Assert
        result.Should().BeOfType<OkObjectResult>("partial success should still return 200 OK");
    }

    [Fact]
    public async Task Enrich_WithSomeInvalidRows_ReturnsOnlyValidRows()
    {
        // Arrange
        var inputTrades = new[]
        {
            TestDataBuilder.CreateValidTradeInput(date: "20260111", productId: "1"),
            TestDataBuilder.CreateValidTradeInput(date: "INVALID", productId: "2"), // Will be discarded
            TestDataBuilder.CreateValidTradeInput(date: "20260113", productId: "3")
        };

        // Service returns only valid trades
        var enrichedTrades = new[]
        {
            TestDataBuilder.CreateEnrichedTradeOutput(date: "20260111"),
            TestDataBuilder.CreateEnrichedTradeOutput(date: "20260113")
        };

        var summary = CreateSummary(totalProcessed: 3, enriched: 2, discarded: 1);

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        var result = await _sut.Enrich(inputTrades);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTrades = okResult.Value.Should().BeAssignableTo<IEnumerable<EnrichedTradeOutputDto>>().Subject.ToList();

        returnedTrades.Should().HaveCount(2, "only valid rows should be returned");
        returnedTrades.Should().OnlyContain(t => t.Date != "INVALID");
    }

    [Fact]
    public async Task Enrich_WithAllRowsDiscarded_ReturnsOkWithEmptyResult()
    {
        // Arrange
        var inputTrades = new[]
        {
            TestDataBuilder.CreateValidTradeInput(date: "INVALID1", productId: "1"),
            TestDataBuilder.CreateValidTradeInput(date: "INVALID2", productId: "2")
        };

        var enrichedTrades = Array.Empty<EnrichedTradeOutputDto>();
        var summary = CreateSummary(totalProcessed: 2, enriched: 0, discarded: 2);

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        var result = await _sut.Enrich(inputTrades);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedTrades = okResult.Value.Should().BeAssignableTo<IEnumerable<EnrichedTradeOutputDto>>().Subject.ToList();

        returnedTrades.Should().BeEmpty("all rows discarded should return empty collection");

        // Verify headers reflect all discarded
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Total-Rows")
            .WhoseValue.ToString().Should().Be("2");
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Enriched-Rows")
            .WhoseValue.ToString().Should().Be("0");
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Discarded-Rows")
            .WhoseValue.ToString().Should().Be("2");
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public async Task Enrich_WithEmptyInput_ReturnsBadRequest()
    {
        // Arrange
        var emptyInput = Array.Empty<TradeInputDto>();

        // Act
        var result = await _sut.Enrich(emptyInput);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
        badRequestResult.Value.ToString()!.Should().ContainEquivalentOf("empty");

        // Verify service was not called
        _mockEnrichmentService.Verify(
            s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()),
            Times.Never);
    }

    [Fact]
    public async Task Enrich_WithNullInput_ReturnsBadRequest()
    {
        // Arrange
        IReadOnlyList<TradeInputDto>? nullInput = null;

        // Act
        var result = await _sut.Enrich(nullInput!);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
        badRequestResult.Value.ToString().Should().ContainEquivalentOf("null");

        // Verify service was not called
        _mockEnrichmentService.Verify(
            s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()),
            Times.Never);
    }

    [Fact]
    public async Task Enrich_WhenServiceThrowsUnexpectedException_ReturnsInternalServerError()
    {
        // Arrange
        var inputTrades = new[] { TestDataBuilder.CreateValidTradeInput() };

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Throws(new ApplicationException("Simulated unexpected failure"));

        // Act
        var result = await _sut.Enrich(inputTrades);

        // Assert
        var statusCodeResult = result.Should().BeOfType<ObjectResult>().Subject;
        statusCodeResult.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        statusCodeResult.Value.Should().NotBeNull();
        statusCodeResult.Value.ToString().Should().ContainEquivalentOf("error");
    }

    [Fact]
    public async Task Enrich_WhenServiceThrowsInvalidOperationException_ReturnsBadRequest()
    {
        // Arrange
        var inputTrades = new[] { TestDataBuilder.CreateValidTradeInput() };

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Throws(new InvalidOperationException("Invalid operation occurred"));

        // Act
        var result = await _sut.Enrich(inputTrades);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.Value.Should().NotBeNull();
        badRequestResult.Value.ToString().Should().ContainEquivalentOf("Invalid operation");
    }

    #endregion

    #region Header Tests

    [Fact]
    public async Task Enrich_ShouldIncludeTotalRowsHeader()
    {
        // Arrange
        var inputTrades = TestDataBuilder.CreateValidTradeBatch(count: 50);
        var enrichedTrades = new[] { TestDataBuilder.CreateEnrichedTradeOutput() };
        var summary = CreateSummary(totalProcessed: 50, enriched: 45, discarded: 5);

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        await _sut.Enrich(inputTrades);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Total-Rows");
        _httpContext.Response.Headers["X-Enrichment-Total-Rows"].ToString().Should().Be("50");
    }

    [Fact]
    public async Task Enrich_ShouldIncludeEnrichedRowsHeader()
    {
        // Arrange
        var inputTrades = new[] { TestDataBuilder.CreateValidTradeInput() };
        var enrichedTrades = new[] { TestDataBuilder.CreateEnrichedTradeOutput() };
        var summary = CreateSummary(totalProcessed: 100, enriched: 95);

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        await _sut.Enrich(inputTrades);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Enriched-Rows");
        _httpContext.Response.Headers["X-Enrichment-Enriched-Rows"].ToString().Should().Be("95");
    }

    [Fact]
    public async Task Enrich_ShouldIncludeDiscardedRowsHeader()
    {
        // Arrange
        var inputTrades = new[] { TestDataBuilder.CreateValidTradeInput() };
        var enrichedTrades = new[] { TestDataBuilder.CreateEnrichedTradeOutput() };
        var summary = CreateSummary(totalProcessed: 100, enriched: 92, discarded: 8);

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        await _sut.Enrich(inputTrades);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Discarded-Rows");
        _httpContext.Response.Headers["X-Enrichment-Discarded-Rows"].ToString().Should().Be("8");
    }

    [Fact]
    public async Task Enrich_ShouldIncludeMissingProductsHeader()
    {
        // Arrange
        var inputTrades = new[] { TestDataBuilder.CreateValidTradeInput() };
        var enrichedTrades = new[] { TestDataBuilder.CreateEnrichedTradeOutput() };
        var summary = CreateSummary(totalProcessed: 100, enriched: 95, missingProducts: 5);

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        await _sut.Enrich(inputTrades);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Missing-Products");
        _httpContext.Response.Headers["X-Enrichment-Missing-Products"].ToString().Should().Be("5");
    }

    [Fact]
    public async Task Enrich_WithMissingProducts_ShouldIncludeMissingProductIdsHeader()
    {
        // Arrange
        var inputTrades = new[] { TestDataBuilder.CreateValidTradeInput() };
        var enrichedTrades = new[] { TestDataBuilder.CreateEnrichedTradeOutput() };
        var summary = CreateSummary(
            totalProcessed: 100,
            enriched: 95,
            missingProducts: 3,
            missingProductIds: new HashSet<int> { 999, 888, 777 });

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        await _sut.Enrich(inputTrades);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Missing-Product-Ids");
        var headerValue = _httpContext.Response.Headers["X-Enrichment-Missing-Product-Ids"].ToString();

        // Header should contain all IDs in sorted order
        headerValue.Should().Contain("777");
        headerValue.Should().Contain("888");
        headerValue.Should().Contain("999");
    }

    [Fact]
    public async Task Enrich_WithNoMissingProducts_ShouldHaveEmptyMissingProductIdsHeader()
    {
        // Arrange
        var inputTrades = new[] { TestDataBuilder.CreateValidTradeInput() };
        var enrichedTrades = new[] { TestDataBuilder.CreateEnrichedTradeOutput() };
        var summary = CreateSummary(
            totalProcessed: 100,
            enriched: 100,
            missingProducts: 0,
            missingProductIds: new HashSet<int>());

        _mockEnrichmentService
            .Setup(s => s.EnrichTrades(It.IsAny<IEnumerable<TradeInputDto>>()))
            .Returns((enrichedTrades, summary));

        // Act
        await _sut.Enrich(inputTrades);

        // Assert
        _httpContext.Response.Headers.Should().ContainKey("X-Enrichment-Missing-Product-Ids");
        _httpContext.Response.Headers["X-Enrichment-Missing-Product-Ids"].ToString().Should().BeEmpty();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates an EnrichmentSummary instance with the specified values.
    /// </summary>
    private static EnrichmentSummary CreateSummary(
        int totalProcessed = 0,
        int enriched = 0,
        int discarded = 0,
        int missingProducts = 0,
        HashSet<int>? missingProductIds = null)
    {
        return new EnrichmentSummary
        {
            TotalRowsProcessed = totalProcessed,
            RowsSuccessfullyEnriched = enriched,
            RowsDiscardedDueToValidation = discarded,
            RowsWithMissingProducts = missingProducts,
            MissingProductIds = missingProductIds ?? new HashSet<int>()
        };
    }

    #endregion
}
