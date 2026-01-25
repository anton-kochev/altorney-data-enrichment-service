using Application.DTOs;
using Application.Mappers;
using Domain.Entities;
using Domain.ValueObjects;
using FluentAssertions;

namespace Application.Tests.Mappers;

public class EnrichedTradeMapperTests
{
    [Fact]
    public void ToDto_WithValidEntity_ReturnsCorrectDto()
    {
        // Arrange
        var date = TradeDate.Create("20231215");
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);
        var entity = EnrichedTrade.Create(date, "Test Product", currency, price);
        var priceString = "99.99";

        // Act
        var dto = EnrichedTradeMapper.ToDto(entity, priceString);

        // Assert
        dto.Date.Should().Be("20231215");
        dto.ProductName.Should().Be("Test Product");
        dto.Currency.Should().Be("USD");
        dto.Price.Should().Be("99.99");
    }

    [Fact]
    public void ToDto_PreservesPriceStringFormat()
    {
        // Arrange
        var date = TradeDate.Create("20231215");
        var currency = Currency.Create("USD");
        var price = Price.Create(100m);
        var entity = EnrichedTrade.Create(date, "Product", currency, price);
        var priceString = "100.00"; // Original format with trailing zeros

        // Act
        var dto = EnrichedTradeMapper.ToDto(entity, priceString);

        // Assert
        dto.Price.Should().Be("100.00", "price string format should be preserved");
    }

    [Theory]
    [InlineData("100")]
    [InlineData("100.0")]
    [InlineData("100.00")]
    [InlineData("0.50")]
    public void ToDto_PreservesVariousPriceFormats(string priceString)
    {
        // Arrange
        var date = TradeDate.Create("20231215");
        var currency = Currency.Create("USD");
        var price = Price.Create(decimal.Parse(priceString));
        var entity = EnrichedTrade.Create(date, "Product", currency, price);

        // Act
        var dto = EnrichedTradeMapper.ToDto(entity, priceString);

        // Assert
        dto.Price.Should().Be(priceString);
    }

    [Fact]
    public void ToDto_UsesFormattedDateFromEntity()
    {
        // Arrange
        var date = TradeDate.Create("  20231215  "); // Input with whitespace
        var currency = Currency.Create("EUR");
        var price = Price.Create(50m);
        var entity = EnrichedTrade.Create(date, "Product", currency, price);

        // Act
        var dto = EnrichedTradeMapper.ToDto(entity, "50");

        // Assert
        dto.Date.Should().Be("20231215", "date should be trimmed");
    }

    [Fact]
    public void ToDto_UsesCurrencyValueFromEntity()
    {
        // Arrange
        var date = TradeDate.Create("20231215");
        var currency = Currency.Create("  GBP  "); // Input with whitespace
        var price = Price.Create(75m);
        var entity = EnrichedTrade.Create(date, "Product", currency, price);

        // Act
        var dto = EnrichedTradeMapper.ToDto(entity, "75");

        // Assert
        dto.Currency.Should().Be("GBP", "currency should be trimmed");
    }

    [Fact]
    public void ToDto_WithMissingProductPlaceholder_ReturnsPlaceholderInDto()
    {
        // Arrange
        var date = TradeDate.Create("20231215");
        var currency = Currency.Create("USD");
        var price = Price.Create(10m);
        var entity = EnrichedTrade.Create(date, "Missing Product Name", currency, price);

        // Act
        var dto = EnrichedTradeMapper.ToDto(entity, "10.00");

        // Assert
        dto.ProductName.Should().Be("Missing Product Name");
    }

    [Fact]
    public void ToDto_WithNullEntity_ThrowsArgumentNullException()
    {
        // Arrange
        EnrichedTrade? entity = null;

        // Act
        var act = () => EnrichedTradeMapper.ToDto(entity!, "10.00");

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("entity");
    }

    [Fact]
    public void ToDto_WithNullPriceString_ThrowsArgumentNullException()
    {
        // Arrange
        var date = TradeDate.Create("20231215");
        var currency = Currency.Create("USD");
        var price = Price.Create(10m);
        var entity = EnrichedTrade.Create(date, "Product", currency, price);

        // Act
        var act = () => EnrichedTradeMapper.ToDto(entity, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("priceString");
    }
}
