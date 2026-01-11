using Application.DTOs;
using Application.Services;
using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Moq;

namespace Infrastructure.Tests.Services;

public class TradeEnrichmentServiceTests : IDisposable
{
    private readonly FakeLogger<TradeEnrichmentService> _fakeLogger;
    private readonly Mock<IProductLookupService> _mockProductLookupService;
    private readonly TradeEnrichmentService _sut;

    public TradeEnrichmentServiceTests()
    {
        _fakeLogger = new FakeLogger<TradeEnrichmentService>();
        _mockProductLookupService = new Mock<IProductLookupService>();
        _sut = new TradeEnrichmentService(_mockProductLookupService.Object, _fakeLogger);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Happy Path Tests

    [Fact]
    public void EnrichTrade_WithValidTradeAndExistingProduct_ShouldReturnEnrichedTrade()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = "99.99"
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(123, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = "Test Product";
                return true;
            });

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().NotBeNull();
        result!.Date.Should().Be("20231215");
        result.ProductName.Should().Be("Test Product");
        result.Currency.Should().Be("USD");
        result.Price.Should().Be("99.99");
    }

    [Fact]
    public void EnrichTrade_ShouldReplaceProductIdWithProductName()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20230101",
            ProductId = "456",
            Currency = "EUR",
            Price = "50.00"
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(456, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = "Sample Product Name";
                return true;
            });

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().NotBeNull();
        result!.ProductName.Should().Be("Sample Product Name");
        result.ProductName.Should().NotBe("456");
    }

    [Fact]
    public void EnrichTrade_ShouldPreserveDateCurrencyPrice()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20240229",
            ProductId = "789",
            Currency = "GBP",
            Price = "123.45"
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(789, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = "Product";
                return true;
            });

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().NotBeNull();
        result!.Date.Should().Be("20240229");
        result.Currency.Should().Be("GBP");
        result.Price.Should().Be("123.45");
    }

    #endregion

    #region Missing Product Tests (US-003)

    [Fact]
    public void EnrichTrade_WithMissingProduct_ShouldUsePlaceholder()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "999",
            Currency = "USD",
            Price = "10.00"
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(999, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = null;
                return false;
            });

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().NotBeNull();
        result!.ProductName.Should().Be("Missing Product Name");
        result.Date.Should().Be("20231215");
        result.Currency.Should().Be("USD");
        result.Price.Should().Be("10.00");
    }

    [Fact]
    public void EnrichTrade_WithMissingProduct_ShouldLogWarning()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "888",
            Currency = "USD",
            Price = "10.00"
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(888, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = null;
                return false;
            });

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().NotBeNull();
        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var warningLog = logRecords.Should().ContainSingle(r => r.Level == LogLevel.Warning).Which;

        // Verify all trade row data using structured logging properties (AC2)
        // This approach is more robust than string matching - survives message format changes
        warningLog.StructuredState.Should().NotBeNull();
        var state = warningLog.StructuredState!.ToDictionary(x => x.Key, x => x.Value);

        state.Should().ContainKey("ProductId").WhoseValue.Should().Be("888");
        state.Should().ContainKey("Date").WhoseValue.Should().Be("20231215");
        state.Should().ContainKey("Currency").WhoseValue.Should().Be("USD");
        state.Should().ContainKey("Price").WhoseValue.Should().Be("10.00");
    }

    [Fact]
    public void EnrichTrades_WithMultipleMissingProducts_ShouldLogEachWarningOnce()
    {
        // Arrange
        var trades = new[]
        {
            new TradeInputDto { Date = "20231215", ProductId = "111", Currency = "USD", Price = "10.00" },
            new TradeInputDto { Date = "20231215", ProductId = "111", Currency = "USD", Price = "20.00" }, // Duplicate
            new TradeInputDto { Date = "20231215", ProductId = "222", Currency = "USD", Price = "30.00" },
            new TradeInputDto { Date = "20231215", ProductId = "222", Currency = "USD", Price = "40.00" }, // Duplicate
            new TradeInputDto { Date = "20231215", ProductId = "333", Currency = "USD", Price = "50.00" }
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(It.IsAny<int>(), out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = null;
                return false;
            });

        // Act
        var (enrichedTrades, summary) = _sut.EnrichTrades(trades);

        // Assert
        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var warningLogs = logRecords.Where(r => r.Level == LogLevel.Warning).ToList();

        // Should log exactly 3 warnings (one for each unique missing product: 111, 222, 333)
        warningLogs.Should().HaveCount(3);
        warningLogs.Should().Contain(r => r.Message.Contains("111"));
        warningLogs.Should().Contain(r => r.Message.Contains("222"));
        warningLogs.Should().Contain(r => r.Message.Contains("333"));
    }

    #endregion

    #region Validation Failure Tests

    [Fact]
    public void EnrichTrade_WithInvalidDateFormat_ShouldReturnNull()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "2023-12-15", // Wrong format (should be yyyyMMdd)
            ProductId = "123",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void EnrichTrade_WithInvalidDateValue_ShouldReturnNull()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231332", // Invalid date (32nd day)
            ProductId = "123",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void EnrichTrade_WithNonIntegerProductId_ShouldReturnNull()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "abc",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void EnrichTrade_WithZeroProductId_ShouldReturnNull()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "0",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void EnrichTrade_WithNegativeProductId_ShouldReturnNull()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "-5",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void EnrichTrade_WithEmptyCurrency_ShouldReturnNull()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "",
            Price = "10.00"
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(123, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = "Product";
                return true;
            });

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void EnrichTrade_WithNegativePrice_ShouldReturnNull()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = "-10.00"
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(123, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = "Product";
                return true;
            });

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void EnrichTrade_WithNonNumericPrice_ShouldReturnNull()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = "abc"
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(123, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = "Product";
                return true;
            });

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region Batch Processing Tests

    [Fact]
    public void EnrichTrades_WithMultipleValidTrades_ShouldEnrichAll()
    {
        // Arrange
        var trades = new[]
        {
            new TradeInputDto { Date = "20231215", ProductId = "1", Currency = "USD", Price = "10.00" },
            new TradeInputDto { Date = "20231216", ProductId = "2", Currency = "EUR", Price = "20.00" },
            new TradeInputDto { Date = "20231217", ProductId = "3", Currency = "GBP", Price = "30.00" }
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(1, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = "Product 1";
                return true;
            });

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(2, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = "Product 2";
                return true;
            });

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(3, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = "Product 3";
                return true;
            });

        // Act
        var (enrichedTrades, summary) = _sut.EnrichTrades(trades);

        // Assert
        var enrichedList = enrichedTrades.ToList();
        enrichedList.Should().HaveCount(3);
        enrichedList[0].ProductName.Should().Be("Product 1");
        enrichedList[1].ProductName.Should().Be("Product 2");
        enrichedList[2].ProductName.Should().Be("Product 3");
    }

    [Fact]
    public void EnrichTrades_WithSomeInvalidTrades_ShouldSkipInvalidAndEnrichValid()
    {
        // Arrange
        var trades = new[]
        {
            new TradeInputDto { Date = "20231215", ProductId = "1", Currency = "USD", Price = "10.00" }, // Valid
            new TradeInputDto { Date = "invalid", ProductId = "2", Currency = "EUR", Price = "20.00" }, // Invalid date
            new TradeInputDto { Date = "20231217", ProductId = "3", Currency = "GBP", Price = "30.00" }, // Valid
            new TradeInputDto { Date = "20231218", ProductId = "abc", Currency = "USD", Price = "40.00" } // Invalid productId
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(It.IsAny<int>(), out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = $"Product {id}";
                return true;
            });

        // Act
        var (enrichedTrades, summary) = _sut.EnrichTrades(trades);

        // Assert
        var enrichedList = enrichedTrades.ToList();
        enrichedList.Should().HaveCount(2);
        enrichedList[0].ProductName.Should().Be("Product 1");
        enrichedList[1].ProductName.Should().Be("Product 3");
    }

    [Fact]
    public void EnrichTrades_WithEmptyInput_ShouldReturnEmptyResultAndZeroSummary()
    {
        // Arrange
        var trades = Array.Empty<TradeInputDto>();

        // Act
        var (enrichedTrades, summary) = _sut.EnrichTrades(trades);

        // Assert
        enrichedTrades.Should().BeEmpty();
        summary.TotalRowsProcessed.Should().Be(0);
        summary.RowsSuccessfullyEnriched.Should().Be(0);
        summary.RowsWithMissingProducts.Should().Be(0);
        summary.RowsDiscardedDueToValidation.Should().Be(0);
        summary.MissingProductIds.Should().BeEmpty();
    }

    #endregion

    #region Summary Statistics Tests

    [Fact]
    public void EnrichTrades_ShouldReturnCorrectTotalRowsProcessed()
    {
        // Arrange
        var trades = new[]
        {
            new TradeInputDto { Date = "20231215", ProductId = "1", Currency = "USD", Price = "10.00" },
            new TradeInputDto { Date = "invalid", ProductId = "2", Currency = "EUR", Price = "20.00" },
            new TradeInputDto { Date = "20231217", ProductId = "3", Currency = "GBP", Price = "30.00" }
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(It.IsAny<int>(), out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = $"Product {id}";
                return true;
            });

        // Act
        var (_, summary) = _sut.EnrichTrades(trades);

        // Assert
        summary.TotalRowsProcessed.Should().Be(3);
    }

    [Fact]
    public void EnrichTrades_ShouldReturnCorrectRowsSuccessfullyEnriched()
    {
        // Arrange
        var trades = new[]
        {
            new TradeInputDto { Date = "20231215", ProductId = "1", Currency = "USD", Price = "10.00" },
            new TradeInputDto { Date = "20231216", ProductId = "2", Currency = "EUR", Price = "20.00" },
            new TradeInputDto { Date = "invalid", ProductId = "3", Currency = "GBP", Price = "30.00" }
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(It.IsAny<int>(), out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = $"Product {id}";
                return true;
            });

        // Act
        var (_, summary) = _sut.EnrichTrades(trades);

        // Assert
        summary.RowsSuccessfullyEnriched.Should().Be(2);
    }

    [Fact]
    public void EnrichTrades_ShouldReturnCorrectRowsWithMissingProducts()
    {
        // Arrange
        var trades = new[]
        {
            new TradeInputDto { Date = "20231215", ProductId = "1", Currency = "USD", Price = "10.00" }, // Found
            new TradeInputDto { Date = "20231216", ProductId = "2", Currency = "EUR", Price = "20.00" }, // Missing
            new TradeInputDto { Date = "20231217", ProductId = "3", Currency = "GBP", Price = "30.00" }  // Missing
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(1, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = "Product 1";
                return true;
            });

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(2, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = null;
                return false;
            });

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(3, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = null;
                return false;
            });

        // Act
        var (_, summary) = _sut.EnrichTrades(trades);

        // Assert
        summary.RowsWithMissingProducts.Should().Be(2);
    }

    [Fact]
    public void EnrichTrades_ShouldReturnCorrectRowsDiscardedDueToValidation()
    {
        // Arrange
        var trades = new[]
        {
            new TradeInputDto { Date = "20231215", ProductId = "1", Currency = "USD", Price = "10.00" }, // Valid
            new TradeInputDto { Date = "invalid", ProductId = "2", Currency = "EUR", Price = "20.00" }, // Invalid date
            new TradeInputDto { Date = "20231217", ProductId = "-3", Currency = "GBP", Price = "30.00" }, // Invalid productId
            new TradeInputDto { Date = "20231218", ProductId = "4", Currency = "", Price = "40.00" } // Invalid currency
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(It.IsAny<int>(), out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = $"Product {id}";
                return true;
            });

        // Act
        var (_, summary) = _sut.EnrichTrades(trades);

        // Assert
        summary.RowsDiscardedDueToValidation.Should().Be(3);
    }

    [Fact]
    public void EnrichTrades_ShouldReturnCorrectMissingProductIds()
    {
        // Arrange
        var trades = new[]
        {
            new TradeInputDto { Date = "20231215", ProductId = "1", Currency = "USD", Price = "10.00" }, // Found
            new TradeInputDto { Date = "20231216", ProductId = "222", Currency = "EUR", Price = "20.00" }, // Missing
            new TradeInputDto { Date = "20231217", ProductId = "333", Currency = "GBP", Price = "30.00" }, // Missing
            new TradeInputDto { Date = "20231218", ProductId = "222", Currency = "USD", Price = "40.00" }  // Missing (duplicate)
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(1, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = "Product 1";
                return true;
            });

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(222, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = null;
                return false;
            });

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(333, out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = null;
                return false;
            });

        // Act
        var (_, summary) = _sut.EnrichTrades(trades);

        // Assert
        summary.MissingProductIds.Should().BeEquivalentTo(new[] { 222, 333 });
    }

    #endregion

    #region Thread Safety Tests

    [Fact]
    public async Task EnrichTrades_WithConcurrentCalls_ShouldHandleCorrectly()
    {
        // Arrange
        var trades = Enumerable.Range(1, 100).Select(i => new TradeInputDto
        {
            Date = "20231215",
            ProductId = i.ToString(),
            Currency = "USD",
            Price = "10.00"
        }).ToArray();

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(It.IsAny<int>(), out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                // Make some products missing to test concurrent logging
                if (id % 10 == 0)
                {
                    name = null;
                    return false;
                }
                name = $"Product {id}";
                return true;
            });

        // Act
        var tasks = Enumerable.Range(0, 10).Select(_ =>
            Task.Run(() => _sut.EnrichTrades(trades))
        ).ToArray();

        var results = await Task.WhenAll(tasks);

        // Assert
        // All tasks should complete successfully
        results.Should().HaveCount(10);

        // Each result should have consistent counts
        foreach (var (enrichedTrades, summary) in results)
        {
            summary.TotalRowsProcessed.Should().Be(100);
            summary.RowsSuccessfullyEnriched.Should().Be(90); // 90 found, 10 missing
            summary.RowsWithMissingProducts.Should().Be(10);
            summary.RowsDiscardedDueToValidation.Should().Be(0);
            summary.MissingProductIds.Should().HaveCount(10);
        }
    }

    #endregion
}
