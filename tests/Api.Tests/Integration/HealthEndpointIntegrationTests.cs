using System.Net;
using System.Text.Json;
using FluentAssertions;

namespace Api.Tests.Integration;

/// <summary>
/// Integration tests for the GET /health endpoint using WebApplicationFactory.
/// These tests verify end-to-end behavior of the health check endpoint.
/// </summary>
public class HealthEndpointIntegrationTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly CustomWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public HealthEndpointIntegrationTests(CustomWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetHealth_ReturnsOk()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "health endpoint should return 200 when service is healthy");
    }

    [Fact]
    public async Task GetHealth_ReturnsJsonContentType()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
    }

    [Fact]
    public async Task GetHealth_ResponseIncludesHealthyStatus()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<HealthResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        healthResponse.Should().NotBeNull();
        healthResponse!.Status.Should().Be("Healthy", "service should be healthy when product data is loaded");
    }

    [Fact]
    public async Task GetHealth_ResponseIncludesProductDataLoaded()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<HealthResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        healthResponse.Should().NotBeNull();
        healthResponse!.ProductDataLoaded.Should().BeTrue("test factory loads product data at startup");
    }

    [Fact]
    public async Task GetHealth_ResponseIncludesProductCount()
    {
        // Act
        var response = await _client.GetAsync("/health");
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<HealthResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        healthResponse.Should().NotBeNull();
        healthResponse!.ProductCount.Should().BeGreaterThan(0, "test factory loads products from TestData/product.csv which contains 5 products");
    }

    [Fact]
    public async Task GetHealth_ResponseIncludesTimestamp()
    {
        // Arrange
        var beforeRequest = DateTime.UtcNow.AddSeconds(-1);

        // Act
        var response = await _client.GetAsync("/health");
        var afterRequest = DateTime.UtcNow.AddSeconds(1);
        var content = await response.Content.ReadAsStringAsync();
        var healthResponse = JsonSerializer.Deserialize<HealthResponse>(content, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        healthResponse.Should().NotBeNull();
        healthResponse!.Timestamp.Should().BeAfter(beforeRequest, "timestamp should be current time");
        healthResponse.Timestamp.Should().BeBefore(afterRequest, "timestamp should be current time");
    }

    /// <summary>
    /// Health response DTO for deserialization in tests.
    /// Matches the expected JSON structure from the health endpoint.
    /// </summary>
    private class HealthResponse
    {
        public string Status { get; set; } = string.Empty;
        public bool ProductDataLoaded { get; set; }
        public int ProductCount { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
