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

    #region Field Preservation Tests (US-004)

    [Theory]
    [InlineData("100", "100")]
    [InlineData("100.0", "100.0")]
    [InlineData("100.00", "100.00")]
    [InlineData("99.99", "99.99")]
    [InlineData("0.50", "0.50")]
    public void EnrichTrade_ShouldPreservePriceStringFormat(string inputPrice, string expectedPrice)
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = inputPrice
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
        result!.Price.Should().Be(expectedPrice);
    }

    [Theory]
    [InlineData("USD", "USD")]
    [InlineData(" USD ", "USD")]
    [InlineData("  EUR  ", "EUR")]
    public void EnrichTrade_ShouldTrimCurrencyWhitespace(string inputCurrency, string expectedCurrency)
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = inputCurrency,
            Price = "100.00"
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
        result!.Currency.Should().Be(expectedCurrency);
    }

    [Theory]
    [InlineData(" 20231215", "20231215")]   // Leading whitespace
    [InlineData("20231215 ", "20231215")]   // Trailing whitespace
    [InlineData(" 20231215 ", "20231215")]  // Both
    [InlineData("  20240229  ", "20240229")] // Multiple spaces, leap year
    public void EnrichTrade_ShouldTrimDateWhitespace(string inputDate, string expectedDate)
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = inputDate,
            ProductId = "123",
            Currency = "USD",
            Price = "100.00"
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
        result!.Date.Should().Be(expectedDate);
    }

    [Theory]
    [InlineData(" 100.00", "100.00")]    // Leading whitespace
    [InlineData("100.00 ", "100.00")]    // Trailing whitespace
    [InlineData(" 100.00 ", "100.00")]   // Both
    [InlineData("  99.99  ", "99.99")]   // Multiple spaces
    public void EnrichTrade_ShouldTrimPriceWhitespace(string inputPrice, string expectedPrice)
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = inputPrice
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
        result!.Price.Should().Be(expectedPrice);
    }

    [Theory]
    [InlineData("20240101")]
    [InlineData("20240229")]  // Leap year
    [InlineData("20001231")]  // Century boundary
    [InlineData("19990101")]
    public void EnrichTrade_ShouldPreserveDateFormat(string inputDate)
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = inputDate,
            ProductId = "123",
            Currency = "USD",
            Price = "100.00"
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
        result!.Date.Should().Be(inputDate);
    }

    [Theory]
    [InlineData("1", "1")]
    [InlineData("0", "0")]
    [InlineData("0.1", "0.1")]
    [InlineData("0.01", "0.01")]
    [InlineData("1000000", "1000000")]
    [InlineData("1000000.99", "1000000.99")]
    public void EnrichTrade_ShouldPreservePriceWithVariousPrecisions(string inputPrice, string expectedPrice)
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = inputPrice
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
        result!.Price.Should().Be(expectedPrice);
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

    [Fact]
    public async Task EnrichTrades_WithHighConcurrency_ShouldLogEachMissingProductOnce()
    {
        // Arrange - Create many trades with same missing product IDs to stress test deduplication
        var trades = Enumerable.Range(1, 100).Select(i => new TradeInputDto
        {
            Date = "20231215",
            ProductId = (i % 5).ToString(), // Only 5 unique product IDs (0-4), repeated 20 times each
            Currency = "USD",
            Price = "10.00"
        }).ToArray();

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(It.IsAny<int>(), out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = null;
                return false; // All products missing
            });

        // Act - Run with higher concurrency to stress test
        var tasks = Enumerable.Range(0, 20).Select(_ =>
            Task.Run(() => _sut.EnrichTrades(trades))
        ).ToArray();

        await Task.WhenAll(tasks);

        // Assert - Should log exactly 5 warnings (one per unique missing product: 0, 1, 2, 3, 4)
        // even though we processed 2000 trades (20 tasks x 100 trades) with only 5 unique IDs
        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var warningLogs = logRecords.Where(r => r.Level == LogLevel.Warning).ToList();

        // Note: Product ID 0 is invalid and gets rejected during validation, so only 1-4 are logged
        warningLogs.Should().HaveCount(4,
            "each unique missing product ID (1-4) should be logged exactly once regardless of concurrency");

        for (int i = 1; i <= 4; i++)
        {
            var productIdToCheck = i.ToString();
            warningLogs.Should().ContainSingle(r =>
                    r.StructuredState != null &&
                    r.StructuredState.Any(s => s.Key == "ProductId" && (s.Value != null && s.Value.ToString() == productIdToCheck)),
                $"product ID {i} should be logged exactly once");
        }
    }

    [Fact]
    public async Task EnrichTrades_WithConcurrentCalls_ShouldNotCorruptFieldValues()
    {
        // Arrange - Create trades with distinct values to detect any field corruption
        var tradesSet1 = new[]
        {
            new TradeInputDto { Date = "20231215", ProductId = "1", Currency = "USD", Price = "111.11" },
            new TradeInputDto { Date = "20231216", ProductId = "2", Currency = "EUR", Price = "222.22" }
        };

        var tradesSet2 = new[]
        {
            new TradeInputDto { Date = "20241215", ProductId = "3", Currency = "GBP", Price = "333.33" },
            new TradeInputDto { Date = "20241216", ProductId = "4", Currency = "JPY", Price = "444.44" }
        };

        _mockProductLookupService
            .Setup(p => p.TryGetProductName(It.IsAny<int>(), out It.Ref<string?>.IsAny))
            .Returns((int id, out string? name) =>
            {
                name = $"Product {id}";
                return true;
            });

        // Act - Run both sets concurrently multiple times
        var tasks = new List<Task<(IEnumerable<EnrichedTradeOutputDto>, EnrichmentSummary)>>();
        for (int i = 0; i < 50; i++)
        {
            tasks.Add(Task.Run(() => _sut.EnrichTrades(tradesSet1)));
            tasks.Add(Task.Run(() => _sut.EnrichTrades(tradesSet2)));
        }

        var results = await Task.WhenAll(tasks);

        // Assert - Verify no field corruption occurred
        var set1Results = results.Where((_, idx) => idx % 2 == 0).ToList();
        var set2Results = results.Where((_, idx) => idx % 2 == 1).ToList();

        foreach (var (enrichedTrades, _) in set1Results)
        {
            var list = enrichedTrades.ToList();
            list.Should().HaveCount(2);
            list[0].Date.Should().Be("20231215");
            list[0].Currency.Should().Be("USD");
            list[0].Price.Should().Be("111.11");
            list[1].Date.Should().Be("20231216");
            list[1].Currency.Should().Be("EUR");
            list[1].Price.Should().Be("222.22");
        }

        foreach (var (enrichedTrades, _) in set2Results)
        {
            var list = enrichedTrades.ToList();
            list.Should().HaveCount(2);
            list[0].Date.Should().Be("20241215");
            list[0].Currency.Should().Be("GBP");
            list[0].Price.Should().Be("333.33");
            list[1].Date.Should().Be("20241216");
            list[1].Currency.Should().Be("JPY");
            list[1].Price.Should().Be("444.44");
        }
    }

    #endregion

    #region Missing Required Fields Tests (US-006)

    [Theory]
    [InlineData("", "123", "USD", "10.00", "date")]
    [InlineData("20231215", "", "USD", "10.00", "productId")]
    [InlineData("20231215", "123", "", "10.00", "currency")]
    [InlineData("20231215", "123", "USD", "", "price")]
    public void EnrichTrade_WithSingleMissingField_ShouldReturnNullAndLogError(
        string date, string productId, string currency, string price, string expectedMissingField)
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = date,
            ProductId = productId,
            Currency = currency,
            Price = price
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull("trades with missing required fields should be discarded");

        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var errorLog = logRecords.Should().ContainSingle(r => r.Level == LogLevel.Error).Which;

        // Verify structured logging properties (more robust than string matching)
        errorLog.StructuredState.Should().NotBeNull();
        var state = errorLog.StructuredState!.ToDictionary(x => x.Key, x => x.Value);

        state.Should().ContainKey("MissingFields").WhoseValue.Should().Contain(expectedMissingField);
        state.Should().ContainKey("RawDate").WhoseValue.Should().Be(date);
        state.Should().ContainKey("RawProductId").WhoseValue.Should().Be(productId);
        state.Should().ContainKey("RawCurrency").WhoseValue.Should().Be(currency);
        state.Should().ContainKey("RawPrice").WhoseValue.Should().Be(price);
    }

    [Fact]
    public void EnrichTrade_WithMultipleMissingFields_ShouldReturnNullAndLogAllMissingFields()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "",
            ProductId = "",
            Currency = "USD",
            Price = ""
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull("trades with missing required fields should be discarded");

        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var errorLog = logRecords.Should().ContainSingle(r => r.Level == LogLevel.Error).Which;

        // Verify structured logging properties
        errorLog.StructuredState.Should().NotBeNull();
        var state = errorLog.StructuredState!.ToDictionary(x => x.Key, x => x.Value);

        var missingFields = state.Should().ContainKey("MissingFields").WhoseValue?.ToString();
        missingFields.Should().Contain("date");
        missingFields.Should().Contain("productId");
        missingFields.Should().Contain("price");
        missingFields.Should().NotContain("currency", "Currency was provided");
    }

    [Theory]
    [InlineData("   ", "123", "USD", "10.00", "date")]
    [InlineData("20231215", "   ", "USD", "10.00", "productId")]
    [InlineData("20231215", "123", "   ", "10.00", "currency")]
    [InlineData("20231215", "123", "USD", "   ", "price")]
    public void EnrichTrade_WithWhitespaceOnlyField_ShouldTreatAsMissingAndLogError(
        string date, string productId, string currency, string price, string expectedMissingField)
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = date,
            ProductId = productId,
            Currency = currency,
            Price = price
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull("whitespace-only fields should be treated as missing");

        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var errorLog = logRecords.Should().ContainSingle(r => r.Level == LogLevel.Error).Which;

        // Verify structured logging properties
        errorLog.StructuredState.Should().NotBeNull();
        var state = errorLog.StructuredState!.ToDictionary(x => x.Key, x => x.Value);

        state.Should().ContainKey("MissingFields").WhoseValue.Should().Contain(expectedMissingField);
    }

    [Theory]
    [InlineData("\t", "123", "USD", "10.00", "date")]
    [InlineData("20231215", "\t", "USD", "10.00", "productId")]
    [InlineData("20231215", "123", "\t", "10.00", "currency")]
    [InlineData("20231215", "123", "USD", "\t", "price")]
    public void EnrichTrade_WithTabOnlyField_ShouldTreatAsMissingAndLogError(
        string date, string productId, string currency, string price, string expectedMissingField)
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = date,
            ProductId = productId,
            Currency = currency,
            Price = price
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull("tab-only fields should be treated as missing");

        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var errorLog = logRecords.Should().ContainSingle(r => r.Level == LogLevel.Error).Which;

        // Verify structured logging properties
        errorLog.StructuredState.Should().NotBeNull();
        var state = errorLog.StructuredState!.ToDictionary(x => x.Key, x => x.Value);

        state.Should().ContainKey("MissingFields").WhoseValue.Should().Contain(expectedMissingField);
    }

    [Theory]
    [InlineData("\n", "123", "USD", "10.00", "date")]
    [InlineData("20231215", "\n", "USD", "10.00", "productId")]
    [InlineData("20231215", "123", "\n", "10.00", "currency")]
    [InlineData("20231215", "123", "USD", "\n", "price")]
    public void EnrichTrade_WithNewlineOnlyField_ShouldTreatAsMissingAndLogError(
        string date, string productId, string currency, string price, string expectedMissingField)
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = date,
            ProductId = productId,
            Currency = currency,
            Price = price
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull("newline-only fields should be treated as missing");

        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var errorLog = logRecords.Should().ContainSingle(r => r.Level == LogLevel.Error).Which;

        // Verify structured logging properties
        errorLog.StructuredState.Should().NotBeNull();
        var state = errorLog.StructuredState!.ToDictionary(x => x.Key, x => x.Value);

        state.Should().ContainKey("MissingFields").WhoseValue.Should().Contain(expectedMissingField);
    }

    [Fact]
    public void EnrichTrade_WithMissingFields_ShouldLogErrorNotWarning()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "",
            ProductId = "123",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();

        var logRecords = _fakeLogger.Collector.GetSnapshot();

        // Should log at Error level, not Warning
        logRecords.Should().ContainSingle(r => r.Level == LogLevel.Error,
            "missing required fields should be logged as errors");
    }

    [Fact]
    public void EnrichTrade_WithMissingFields_ShouldIncludeAllRawInputValuesInLog()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "invalid-date",
            ProductId = "",
            Currency = "EUR",
            Price = "99.99"
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();

        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var errorLog = logRecords.Should().ContainSingle(r => r.Level == LogLevel.Error).Which;

        // Verify all raw input values are included in structured state
        errorLog.StructuredState.Should().NotBeNull();
        var state = errorLog.StructuredState!.ToDictionary(x => x.Key, x => x.Value);

        state.Should().ContainKey("RawDate").WhoseValue.Should().Be("invalid-date");
        state.Should().ContainKey("RawProductId").WhoseValue.Should().Be("");
        state.Should().ContainKey("RawCurrency").WhoseValue.Should().Be("EUR");
        state.Should().ContainKey("RawPrice").WhoseValue.Should().Be("99.99");
    }

    [Fact]
    public void EnrichTrades_WithMixOfValidAndMissingFieldRecords_ShouldDiscardInvalidOnes()
    {
        // Arrange
        var trades = new[]
        {
            new TradeInputDto { Date = "20231215", ProductId = "1", Currency = "USD", Price = "10.00" }, // Valid
            new TradeInputDto { Date = "", ProductId = "2", Currency = "EUR", Price = "20.00" }, // Missing Date
            new TradeInputDto { Date = "20231217", ProductId = "3", Currency = "GBP", Price = "30.00" }, // Valid
            new TradeInputDto { Date = "20231218", ProductId = "", Currency = "USD", Price = "40.00" }, // Missing ProductId
            new TradeInputDto { Date = "20231219", ProductId = "5", Currency = "", Price = "50.00" }, // Missing Currency
            new TradeInputDto { Date = "20231220", ProductId = "6", Currency = "JPY", Price = "" } // Missing Price
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
        enrichedList.Should().HaveCount(2, "only 2 trades have all required fields");
        enrichedList[0].ProductName.Should().Be("Product 1");
        enrichedList[1].ProductName.Should().Be("Product 3");
    }

    [Fact]
    public void EnrichTrades_WithMissingFieldRecords_ShouldIncrementRowsDiscardedCounter()
    {
        // Arrange
        var trades = new[]
        {
            new TradeInputDto { Date = "20231215", ProductId = "1", Currency = "USD", Price = "10.00" }, // Valid
            new TradeInputDto { Date = "", ProductId = "2", Currency = "EUR", Price = "20.00" }, // Missing Date
            new TradeInputDto { Date = "20231217", ProductId = "", Currency = "GBP", Price = "30.00" }, // Missing ProductId
            new TradeInputDto { Date = "20231218", ProductId = "4", Currency = "", Price = "40.00" } // Missing Currency
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
        summary.TotalRowsProcessed.Should().Be(4);
        summary.RowsSuccessfullyEnriched.Should().Be(1);
        summary.RowsDiscardedDueToValidation.Should().Be(3,
            "three trades had missing required fields and should be discarded");
    }

    [Fact]
    public void EnrichTrades_WithOnlyMissingFieldRecords_ShouldReturnEmptyEnrichedList()
    {
        // Arrange
        var trades = new[]
        {
            new TradeInputDto { Date = "", ProductId = "1", Currency = "USD", Price = "10.00" },
            new TradeInputDto { Date = "20231215", ProductId = "", Currency = "EUR", Price = "20.00" },
            new TradeInputDto { Date = "20231216", ProductId = "2", Currency = "", Price = "30.00" },
            new TradeInputDto { Date = "20231217", ProductId = "3", Currency = "GBP", Price = "" }
        };

        // Act
        var (enrichedTrades, summary) = _sut.EnrichTrades(trades);

        // Assert
        enrichedTrades.Should().BeEmpty("all trades had missing required fields");
        summary.TotalRowsProcessed.Should().Be(4);
        summary.RowsSuccessfullyEnriched.Should().Be(0);
        summary.RowsDiscardedDueToValidation.Should().Be(4);
    }

    [Fact]
    public void EnrichTrade_WithAllFieldsMissing_ShouldLogAllFieldsInMissingFieldsList()
    {
        // Arrange
        var tradeInput = new TradeInputDto
        {
            Date = "",
            ProductId = "",
            Currency = "",
            Price = ""
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();

        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var errorLog = logRecords.Should().ContainSingle(r => r.Level == LogLevel.Error).Which;

        // Verify all fields are listed as missing
        errorLog.StructuredState.Should().NotBeNull();
        var state = errorLog.StructuredState!.ToDictionary(x => x.Key, x => x.Value);

        var missingFields = state.Should().ContainKey("MissingFields").WhoseValue?.ToString();
        missingFields.Should().Contain("date");
        missingFields.Should().Contain("productId");
        missingFields.Should().Contain("currency");
        missingFields.Should().Contain("price");
    }

    [Fact]
    public void EnrichTrade_WithMissingFieldsBeforeInvalidFormat_ShouldLogMissingFieldsNotFormatError()
    {
        // Arrange - Price would be invalid format if we got that far
        var tradeInput = new TradeInputDto
        {
            Date = "",  // Missing field - should be caught first
            ProductId = "123",
            Currency = "USD",
            Price = "not-a-number"  // Would fail format validation
        };

        // Act
        var result = _sut.EnrichTrade(tradeInput);

        // Assert
        result.Should().BeNull();

        var logRecords = _fakeLogger.Collector.GetSnapshot();
        var errorLog = logRecords.Should().ContainSingle(r => r.Level == LogLevel.Error,
            "should log missing field error, not format error").Which;

        // Verify it's logging missing Date, not price format error
        errorLog.StructuredState.Should().NotBeNull();
        var state = errorLog.StructuredState!.ToDictionary(x => x.Key, x => x.Value);

        state.Should().ContainKey("MissingFields").WhoseValue.Should().Contain("date");
    }

    #endregion
}
