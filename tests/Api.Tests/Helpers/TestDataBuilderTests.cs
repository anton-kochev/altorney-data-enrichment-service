using Api.Tests.Helpers;
using FluentAssertions;

namespace Api.Tests.Helpers;

/// <summary>
/// Tests for TestDataBuilder helper class.
/// Validates that test data builders create correctly structured test data.
/// </summary>
public sealed class TestDataBuilderTests
{
    [Fact]
    public void CreateValidTradeInput_WithDefaults_ReturnsValidDto()
    {
        // Act
        var result = TestDataBuilder.CreateValidTradeInput();

        // Assert
        result.Should().NotBeNull();
        result.Date.Should().Be("20260111");
        result.ProductId.Should().Be("1");
        result.Currency.Should().Be("USD");
        result.Price.Should().Be("100.50");
    }

    [Fact]
    public void CreateValidTradeInput_WithCustomValues_ReturnsCustomDto()
    {
        // Act
        var result = TestDataBuilder.CreateValidTradeInput(
            date: "20250115",
            productId: "42",
            currency: "EUR",
            price: "250.75"
        );

        // Assert
        result.Should().NotBeNull();
        result.Date.Should().Be("20250115");
        result.ProductId.Should().Be("42");
        result.Currency.Should().Be("EUR");
        result.Price.Should().Be("250.75");
    }

    [Fact]
    public void CreateEnrichedTradeOutput_WithDefaults_ReturnsValidDto()
    {
        // Act
        var result = TestDataBuilder.CreateEnrichedTradeOutput();

        // Assert
        result.Should().NotBeNull();
        result.Date.Should().Be("20260111");
        result.ProductName.Should().Be("Treasury Bills Domestic");
        result.Currency.Should().Be("USD");
        result.Price.Should().Be("100.50");
    }

    [Fact]
    public void CreateEnrichedTradeOutput_WithCustomValues_ReturnsCustomDto()
    {
        // Act
        var result = TestDataBuilder.CreateEnrichedTradeOutput(
            date: "20250115",
            productName: "Corporate Bonds",
            currency: "GBP",
            price: "500.00"
        );

        // Assert
        result.Should().NotBeNull();
        result.Date.Should().Be("20250115");
        result.ProductName.Should().Be("Corporate Bonds");
        result.Currency.Should().Be("GBP");
        result.Price.Should().Be("500.00");
    }

    [Fact]
    public void CreateCsvContent_WithSingleTrade_ReturnsValidCsv()
    {
        // Arrange
        var trade = TestDataBuilder.CreateValidTradeInput(
            date: "20260111",
            productId: "1",
            currency: "USD",
            price: "100.50"
        );

        // Act
        var csv = TestDataBuilder.CreateCsvContent(trade);

        // Assert
        csv.Should().NotBeNullOrEmpty();
        csv.Should().Contain("date,product_id,currency,price");
        csv.Should().Contain("20260111,1,USD,100.50");
    }

    [Fact]
    public void CreateCsvContent_WithMultipleTrades_ReturnsValidCsv()
    {
        // Arrange
        var trade1 = TestDataBuilder.CreateValidTradeInput(
            date: "20260111",
            productId: "1",
            currency: "USD",
            price: "100.50"
        );
        var trade2 = TestDataBuilder.CreateValidTradeInput(
            date: "20260112",
            productId: "2",
            currency: "EUR",
            price: "200.75"
        );

        // Act
        var csv = TestDataBuilder.CreateCsvContent(trade1, trade2);

        // Assert
        csv.Should().NotBeNullOrEmpty();
        csv.Should().Contain("date,product_id,currency,price");
        csv.Should().Contain("20260111,1,USD,100.50");
        csv.Should().Contain("20260112,2,EUR,200.75");
    }

    [Fact]
    public void CreateCsvContent_WithNoTrades_ReturnsHeaderOnly()
    {
        // Act
        var csv = TestDataBuilder.CreateCsvContent();

        // Assert
        csv.Should().Be("date,product_id,currency,price\n");
    }

    [Fact]
    public void CreateEmptyCsvContent_ReturnsHeaderOnly()
    {
        // Act
        var csv = TestDataBuilder.CreateEmptyCsvContent();

        // Assert
        csv.Should().Be("date,product_id,currency,price\n");
    }

    [Fact]
    public void CreateCsvContentWithCustomHeader_UsesProvidedHeader()
    {
        // Arrange
        var customHeader = "Date,ProdId,Curr,Amount";
        var trade = TestDataBuilder.CreateValidTradeInput();

        // Act
        var csv = TestDataBuilder.CreateCsvContentWithCustomHeader(customHeader, trade);

        // Assert
        csv.Should().Contain(customHeader);
        csv.Should().Contain("20260111,1,USD,100.50");
    }

    [Fact]
    public void CreateCsvContentWithRawRows_IncludesProvidedRows()
    {
        // Act
        var csv = TestDataBuilder.CreateCsvContentWithRawRows(
            "20260111,1,USD,100.50",
            "invalid,data,row",
            "20260112,2,EUR,200.75"
        );

        // Assert
        csv.Should().Contain("date,product_id,currency,price");
        csv.Should().Contain("20260111,1,USD,100.50");
        csv.Should().Contain("invalid,data,row");
        csv.Should().Contain("20260112,2,EUR,200.75");
    }

    [Fact]
    public void CreateValidTradeBatch_CreatesCorrectNumberOfTrades()
    {
        // Act
        var trades = TestDataBuilder.CreateValidTradeBatch(count: 5);

        // Assert
        trades.Should().HaveCount(5);
    }

    [Fact]
    public void CreateValidTradeBatch_GeneratesSequentialProductIds()
    {
        // Act
        var trades = TestDataBuilder.CreateValidTradeBatch(count: 3, startProductId: 10);

        // Assert
        trades[0].ProductId.Should().Be("10");
        trades[1].ProductId.Should().Be("11");
        trades[2].ProductId.Should().Be("12");
    }

    [Fact]
    public void CreateValidTradeBatch_AlternatesCurrencies()
    {
        // Act
        var trades = TestDataBuilder.CreateValidTradeBatch(count: 4);

        // Assert
        trades[0].Currency.Should().Be("USD");
        trades[1].Currency.Should().Be("EUR");
        trades[2].Currency.Should().Be("USD");
        trades[3].Currency.Should().Be("EUR");
    }

    [Fact]
    public void CreateValidTradeBatch_GeneratesIncrementingPrices()
    {
        // Act
        var trades = TestDataBuilder.CreateValidTradeBatch(count: 3);

        // Assert
        trades[0].Price.Should().Be("100.00");
        trades[1].Price.Should().Be("110.00");
        trades[2].Price.Should().Be("120.00");
    }

    [Fact]
    public void CreateValidTradeBatch_UsesSameDateForAllTrades()
    {
        // Act
        var trades = TestDataBuilder.CreateValidTradeBatch(
            count: 3,
            startDate: "20250615"
        );

        // Assert
        trades.Should().AllSatisfy(trade => trade.Date.Should().Be("20250615"));
    }
}
