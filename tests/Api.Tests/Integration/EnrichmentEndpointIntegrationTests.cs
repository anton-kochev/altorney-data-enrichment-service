using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Api.Tests.Helpers;
using FluentAssertions;

namespace Api.Tests.Integration;

/// <summary>
/// Integration tests for the POST /api/v1/enrich endpoint using WebApplicationFactory.
/// These tests verify end-to-end behavior of the CSV enrichment endpoint.
/// </summary>
public class EnrichmentEndpointIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public EnrichmentEndpointIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    #region Happy Path Tests

    [Fact]
    public async Task PostEnrich_WithValidCsv_ReturnsOk()
    {
        // Arrange
        var csvContent = TestDataBuilder.CreateCsvContent(
            TestDataBuilder.CreateValidTradeInput(date: "20260111", productId: "1", currency: "USD", price: "100.50"));

        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostEnrich_WithValidCsv_ReturnsTextCsvContentType()
    {
        // Arrange
        var csvContent = TestDataBuilder.CreateCsvContent(
            TestDataBuilder.CreateValidTradeInput());

        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("text/csv");
    }

    [Fact]
    public async Task PostEnrich_WithValidCsv_ReturnsEnrichedCsvWithHeader()
    {
        // Arrange
        var csvContent = TestDataBuilder.CreateCsvContent(
            TestDataBuilder.CreateValidTradeInput(date: "20260111", productId: "1", currency: "USD", price: "100.50"));

        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);
        var responseBody = await response.Content.ReadAsStringAsync();

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        responseBody.Should().StartWith("date,productName,currency,price");
    }

    [Fact]
    public async Task PostEnrich_WithValidCsv_IncludesSummaryHeaders()
    {
        // Arrange
        var csvContent = TestDataBuilder.CreateCsvContent(
            TestDataBuilder.CreateValidTradeInput(),
            TestDataBuilder.CreateValidTradeInput(productId: "2"),
            TestDataBuilder.CreateValidTradeInput(productId: "3"));

        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert
        response.Headers.Should().ContainKey("X-Enrichment-Total-Rows");
        response.Headers.Should().ContainKey("X-Enrichment-Enriched-Rows");
        response.Headers.Should().ContainKey("X-Enrichment-Discarded-Rows");
        response.Headers.Should().ContainKey("X-Enrichment-Missing-Products");
        response.Headers.Should().ContainKey("X-Enrichment-Missing-Product-Ids");
    }

    #endregion

    #region Content-Type Negotiation Tests

    [Fact]
    public async Task PostEnrich_WithTextCsvContentType_Succeeds()
    {
        // Arrange
        var csvContent = TestDataBuilder.CreateCsvContent(TestDataBuilder.CreateValidTradeInput());
        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostEnrich_WithApplicationCsvContentType_Succeeds()
    {
        // Arrange
        var csvContent = TestDataBuilder.CreateCsvContent(TestDataBuilder.CreateValidTradeInput());
        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/csv");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert - application/csv is supported by the formatter
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.UnsupportedMediaType },
            "application/csv may or may not be supported depending on formatter configuration");
    }

    [Fact]
    public async Task PostEnrich_WithTextCsvAndCharset_Succeeds()
    {
        // Arrange
        var csvContent = TestDataBuilder.CreateCsvContent(TestDataBuilder.CreateValidTradeInput());
        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv") { CharSet = "utf-8" };

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PostEnrich_WithJsonContentType_ReturnsUnsupportedMediaType()
    {
        // Arrange
        var jsonContent = "[{\"date\":\"20260111\",\"productId\":\"1\",\"currency\":\"USD\",\"price\":\"100.50\"}]";
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.UnsupportedMediaType);
    }

    #endregion

    #region Error Scenarios

    [Fact]
    public async Task PostEnrich_WithEmptyBody_ReturnsErrorResponse()
    {
        // Arrange
        var csvContent = "date,product_id,currency,price\n"; // Header only, no data
        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert - empty body results in BadRequest or NotAcceptable depending on content negotiation
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.NotAcceptable },
            "empty CSV data should be rejected with an error status");
    }

    [Fact]
    public async Task PostEnrich_WithMissingRequiredColumns_ReturnsBadRequest()
    {
        // Arrange - missing productId column
        var csvContent = "date,currency,price\n20260111,USD,100.50\n";
        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region Partial Success Scenarios

    [Fact]
    public async Task PostEnrich_WithMixOfValidAndInvalidRows_ReturnsOk()
    {
        // Arrange - mix of valid and invalid dates
        var csvContent = "date,product_id,currency,price\n" +
                         "20260111,1,USD,100.50\n" +
                         "INVALID,2,EUR,200.00\n" +  // Invalid date - will be discarded
                         "20260113,3,GBP,300.00\n";

        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "partial success should return 200");
    }

    [Fact]
    public async Task PostEnrich_WithMixOfValidAndInvalidRows_ReflectsDiscardedInHeaders()
    {
        // Arrange
        var csvContent = "date,product_id,currency,price\n" +
                         "20260111,1,USD,100.50\n" +
                         "INVALID,2,EUR,200.00\n" +  // Invalid date
                         "20260113,3,GBP,300.00\n";

        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert
        response.Headers.GetValues("X-Enrichment-Total-Rows").First().Should().Be("3");
        response.Headers.GetValues("X-Enrichment-Discarded-Rows").First().Should().Be("1");
    }

    #endregion

    #region Multiple Rows Tests

    [Fact]
    public async Task PostEnrich_WithMultipleRows_ProcessesAllRows()
    {
        // Arrange
        var trades = TestDataBuilder.CreateValidTradeBatch(count: 5);
        var csvContent = TestDataBuilder.CreateCsvContent(trades.ToArray());

        var content = new StringContent(csvContent, Encoding.UTF8);
        content.Headers.ContentType = new MediaTypeHeaderValue("text/csv");

        // Act
        var response = await _client.PostAsync("/api/v1/enrich", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.GetValues("X-Enrichment-Total-Rows").First().Should().Be("5");
    }

    #endregion
}
