using Application.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Api.Tests.Controllers;

/// <summary>
/// Unit tests for HealthController following TDD approach.
/// These tests define the expected behavior of the health check endpoint (GET /health).
/// Tests verify health status reporting based on product data loading state.
/// </summary>
public class HealthControllerTests : IDisposable
{
    private readonly FakeLogger<Api.Controllers.HealthController> _fakeLogger;
    private readonly Mock<IProductLookupService> _mockProductLookupService;
    private readonly Api.Controllers.HealthController _sut;

    public HealthControllerTests()
    {
        _fakeLogger = new FakeLogger<Api.Controllers.HealthController>();
        _mockProductLookupService = new Mock<IProductLookupService>();
        _sut = new Api.Controllers.HealthController(_fakeLogger, _mockProductLookupService.Object);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Happy Path - Product Data Loaded

    [Fact]
    public void GetHealth_WhenProductDataLoaded_ReturnsOkResult()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(true);
        _mockProductLookupService.Setup(p => p.Count).Returns(10000);

        // Act
        var result = _sut.GetHealth();

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.StatusCode.Should().Be(StatusCodes.Status200OK);
    }

    [Fact]
    public void GetHealth_WhenProductDataLoaded_ReturnsHealthyStatus()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(true);
        _mockProductLookupService.Setup(p => p.Count).Returns(50000);

        // Act
        var result = _sut.GetHealth();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<Application.DTOs.HealthCheckResponseDto>().Subject;

        response.Status.Should().Be("Healthy");
        response.ProductDataLoaded.Should().BeTrue();
    }

    [Fact]
    public void GetHealth_WhenProductDataLoaded_ResponseIncludesProductCount()
    {
        // Arrange
        const int expectedProductCount = 99999;
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(true);
        _mockProductLookupService.Setup(p => p.Count).Returns(expectedProductCount);

        // Act
        var result = _sut.GetHealth();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<Application.DTOs.HealthCheckResponseDto>().Subject;

        response.ProductCount.Should().Be(expectedProductCount);
    }

    [Fact]
    public void GetHealth_WhenProductDataLoaded_ResponseIncludesTimestamp()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(true);
        _mockProductLookupService.Setup(p => p.Count).Returns(1000);

        // Act
        var result = _sut.GetHealth();
        var afterCall = DateTime.UtcNow;

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<Application.DTOs.HealthCheckResponseDto>().Subject;

        response.Timestamp.Should().NotBe(default(DateTime));
        response.Timestamp.Should().BeOnOrAfter(beforeCall).And.BeOnOrBefore(afterCall);
    }

    [Fact]
    public void GetHealth_WhenProductDataLoaded_LogsHealthCheckExecution()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(true);
        _mockProductLookupService.Setup(p => p.Count).Returns(5000);

        // Act
        _sut.GetHealth();

        // Assert - Use StructuredState for robust, format-independent assertions
        var logEntries = _fakeLogger.Collector.GetSnapshot();
        logEntries.Should().ContainSingle(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Information);

        var logEntry = logEntries.First(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Information);
        logEntry.StructuredState.Should().NotBeNull();
        var state = logEntry.StructuredState!.ToDictionary(x => x.Key, x => x.Value);

        state.Should().ContainKey("Status").WhoseValue.Should().Be("Healthy");
        state.Should().ContainKey("ProductDataLoaded").WhoseValue.Should().Be("True");
        state.Should().ContainKey("ProductCount").WhoseValue.Should().Be("5000");
    }

    [Fact]
    public void GetHealth_WhenProductDataLoaded_WithZeroProducts_ReturnsHealthyStatus()
    {
        // Arrange - Edge case: IsLoaded = true but Count = 0
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(true);
        _mockProductLookupService.Setup(p => p.Count).Returns(0);

        // Act
        var result = _sut.GetHealth();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<Application.DTOs.HealthCheckResponseDto>().Subject;

        response.Status.Should().Be("Healthy");
        response.ProductDataLoaded.Should().BeTrue();
        response.ProductCount.Should().Be(0);
    }

    #endregion

    #region Unhealthy Path - Product Data Not Loaded

    [Fact]
    public void GetHealth_WhenProductDataNotLoaded_ReturnsServiceUnavailable()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(false);
        _mockProductLookupService.Setup(p => p.Count).Returns(0);

        // Act
        var result = _sut.GetHealth();

        // Assert
        result.Should().BeOfType<ObjectResult>();
        var objectResult = result as ObjectResult;
        objectResult!.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);
    }

    [Fact]
    public void GetHealth_WhenProductDataNotLoaded_ReturnsUnhealthyStatus()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(false);
        _mockProductLookupService.Setup(p => p.Count).Returns(0);

        // Act
        var result = _sut.GetHealth();

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(StatusCodes.Status503ServiceUnavailable);

        var response = objectResult.Value.Should().BeAssignableTo<Application.DTOs.HealthCheckResponseDto>().Subject;

        response.Status.Should().Be("Unhealthy");
        response.ProductDataLoaded.Should().BeFalse();
    }

    [Fact]
    public void GetHealth_WhenProductDataNotLoaded_ResponseIncludesZeroProductCount()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(false);
        _mockProductLookupService.Setup(p => p.Count).Returns(0);

        // Act
        var result = _sut.GetHealth();

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        var response = objectResult.Value.Should().BeAssignableTo<Application.DTOs.HealthCheckResponseDto>().Subject;

        response.ProductCount.Should().Be(0);
    }

    [Fact]
    public void GetHealth_WhenProductDataNotLoaded_ResponseIncludesTimestamp()
    {
        // Arrange
        var beforeCall = DateTime.UtcNow;
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(false);
        _mockProductLookupService.Setup(p => p.Count).Returns(0);

        // Act
        var result = _sut.GetHealth();
        var afterCall = DateTime.UtcNow;

        // Assert
        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        var response = objectResult.Value.Should().BeAssignableTo<Application.DTOs.HealthCheckResponseDto>().Subject;

        response.Timestamp.Should().NotBe(default(DateTime));
        response.Timestamp.Should().BeOnOrAfter(beforeCall).And.BeOnOrBefore(afterCall);
    }

    [Fact]
    public void GetHealth_WhenProductDataNotLoaded_LogsUnhealthyStatus()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(false);
        _mockProductLookupService.Setup(p => p.Count).Returns(0);

        // Act
        _sut.GetHealth();

        // Assert - Use StructuredState for robust, format-independent assertions
        var logEntries = _fakeLogger.Collector.GetSnapshot();
        logEntries.Should().ContainSingle(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning);

        var logEntry = logEntries.First(entry => entry.Level == Microsoft.Extensions.Logging.LogLevel.Warning);
        logEntry.StructuredState.Should().NotBeNull();
        var state = logEntry.StructuredState!.ToDictionary(x => x.Key, x => x.Value);

        state.Should().ContainKey("Status").WhoseValue.Should().Be("Unhealthy");
        state.Should().ContainKey("ProductDataLoaded").WhoseValue.Should().Be("False");
    }

    #endregion

    #region Service Interaction Tests

    [Fact]
    public void GetHealth_CallsProductLookupService_IsLoadedProperty()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(true);
        _mockProductLookupService.Setup(p => p.Count).Returns(1000);

        // Act
        _sut.GetHealth();

        // Assert
        _mockProductLookupService.Verify(p => p.IsLoaded, Times.Once);
    }

    [Fact]
    public void GetHealth_CallsProductLookupService_CountProperty()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(true);
        _mockProductLookupService.Setup(p => p.Count).Returns(2000);

        // Act
        _sut.GetHealth();

        // Assert
        _mockProductLookupService.Verify(p => p.Count, Times.Once);
    }

    [Fact]
    public void GetHealth_DoesNotCallOtherProductLookupServiceMethods()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(true);
        _mockProductLookupService.Setup(p => p.Count).Returns(3000);

        // Act
        _sut.GetHealth();

        // Assert - Ensure only properties are accessed, not methods like GetProductName
        _mockProductLookupService.Verify(
            p => p.GetProductName(It.IsAny<int>()),
            Times.Never,
            "Health check should not call GetProductName");

        _mockProductLookupService.Verify(
            p => p.TryGetProductName(It.IsAny<int>(), out It.Ref<string?>.IsAny),
            Times.Never,
            "Health check should not call TryGetProductName");
    }

    #endregion

    #region Response Structure Tests

    [Fact]
    public void GetHealth_ResponseContainsAllRequiredFields()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(true);
        _mockProductLookupService.Setup(p => p.Count).Returns(12345);

        // Act
        var result = _sut.GetHealth();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = okResult.Value.Should().BeAssignableTo<Application.DTOs.HealthCheckResponseDto>().Subject;

        // Verify all required fields are present and have values
        response.Status.Should().NotBeNullOrEmpty();
        response.ProductDataLoaded.Should().BeTrue();
        response.ProductCount.Should().BeGreaterThanOrEqualTo(0);
        response.Timestamp.Should().NotBe(default(DateTime));
    }

    [Fact]
    public void GetHealth_MultipleInvocations_ReturnConsistentStatus()
    {
        // Arrange
        _mockProductLookupService.Setup(p => p.IsLoaded).Returns(true);
        _mockProductLookupService.Setup(p => p.Count).Returns(7500);

        // Act
        var result1 = _sut.GetHealth();
        var result2 = _sut.GetHealth();
        var result3 = _sut.GetHealth();

        // Assert
        var response1 = (result1 as OkObjectResult)!.Value as Application.DTOs.HealthCheckResponseDto;
        var response2 = (result2 as OkObjectResult)!.Value as Application.DTOs.HealthCheckResponseDto;
        var response3 = (result3 as OkObjectResult)!.Value as Application.DTOs.HealthCheckResponseDto;

        response1!.Status.Should().Be("Healthy");
        response2!.Status.Should().Be("Healthy");
        response3!.Status.Should().Be("Healthy");

        response1.ProductCount.Should().Be(7500);
        response2.ProductCount.Should().Be(7500);
        response3.ProductCount.Should().Be(7500);
    }

    #endregion
}
