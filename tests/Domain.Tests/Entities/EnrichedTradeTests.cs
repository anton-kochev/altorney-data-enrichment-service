using FluentAssertions;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Tests.Entities;

public class EnrichedTradeTests
{
    #region Create - Valid Data Tests

    [Fact]
    public void Create_WithValidData_ShouldSucceed()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Treasury Bill";
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        // Act
        var enrichedTrade = EnrichedTrade.Create(date, productName, currency, price);

        // Assert
        enrichedTrade.Should().NotBeNull();
        enrichedTrade.Date.Should().Be(date);
        enrichedTrade.ProductName.Should().Be(productName);
        enrichedTrade.Currency.Should().Be(currency);
        enrichedTrade.Price.Should().Be(price);
    }

    [Fact]
    public void Create_WithEmptyProductName_ShouldSucceed()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = string.Empty;
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        // Act
        var enrichedTrade = EnrichedTrade.Create(date, productName, currency, price);

        // Assert
        enrichedTrade.Should().NotBeNull();
        enrichedTrade.ProductName.Should().BeEmpty();
    }

    [Fact]
    public void ProductName_CanBeMissingProductNamePlaceholder()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Missing Product Name";
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        // Act
        var enrichedTrade = EnrichedTrade.Create(date, productName, currency, price);

        // Assert
        enrichedTrade.Should().NotBeNull();
        enrichedTrade.ProductName.Should().Be("Missing Product Name");
    }

    #endregion

    #region Create - Null Argument Tests

    [Fact]
    public void Create_WithNullDate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var productName = "Treasury Bill";
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        // Act
        var act = () => EnrichedTrade.Create(null!, productName, currency, price);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("date");
    }

    [Fact]
    public void Create_WithNullProductName_ShouldThrowArgumentNullException()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        // Act
        var act = () => EnrichedTrade.Create(date, null!, currency, price);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("productName");
    }

    [Fact]
    public void Create_WithNullCurrency_ShouldThrowArgumentNullException()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Treasury Bill";
        var price = Price.Create(99.99m);

        // Act
        var act = () => EnrichedTrade.Create(date, productName, null!, price);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("currency");
    }

    [Fact]
    public void Create_WithNullPrice_ShouldThrowArgumentNullException()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Treasury Bill";
        var currency = Currency.Create("USD");

        // Act
        var act = () => EnrichedTrade.Create(date, productName, currency, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("price");
    }

    #endregion

    #region Property Tests

    [Fact]
    public void Date_ShouldReturnCorrectValue()
    {
        // Arrange
        var expectedDate = TradeDate.Create("20250615");
        var productName = "Corporate Bond";
        var currency = Currency.Create("EUR");
        var price = Price.Create(150.50m);

        // Act
        var enrichedTrade = EnrichedTrade.Create(expectedDate, productName, currency, price);

        // Assert
        enrichedTrade.Date.Should().Be(expectedDate);
        enrichedTrade.Date.Value.Year.Should().Be(2025);
        enrichedTrade.Date.Value.Month.Should().Be(6);
        enrichedTrade.Date.Value.Day.Should().Be(15);
    }

    [Fact]
    public void ProductName_ShouldReturnCorrectValue()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var expectedProductName = "Government Bond";
        var currency = Currency.Create("GBP");
        var price = Price.Create(200.00m);

        // Act
        var enrichedTrade = EnrichedTrade.Create(date, expectedProductName, currency, price);

        // Assert
        enrichedTrade.ProductName.Should().Be(expectedProductName);
    }

    [Fact]
    public void Currency_ShouldReturnCorrectValue()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Municipal Bond";
        var expectedCurrency = Currency.Create("JPY");
        var price = Price.Create(10000m);

        // Act
        var enrichedTrade = EnrichedTrade.Create(date, productName, expectedCurrency, price);

        // Assert
        enrichedTrade.Currency.Should().Be(expectedCurrency);
        enrichedTrade.Currency.Value.Should().Be("JPY");
    }

    [Fact]
    public void Price_ShouldReturnCorrectValue()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Zero Coupon Bond";
        var currency = Currency.Create("USD");
        var expectedPrice = Price.Create(500.75m);

        // Act
        var enrichedTrade = EnrichedTrade.Create(date, productName, currency, expectedPrice);

        // Assert
        enrichedTrade.Price.Should().Be(expectedPrice);
        enrichedTrade.Price.Value.Should().Be(500.75m);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_TwoEnrichedTradesWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Treasury Bill";
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        var enrichedTrade1 = EnrichedTrade.Create(date, productName, currency, price);
        var enrichedTrade2 = EnrichedTrade.Create(date, productName, currency, price);

        // Act & Assert
        enrichedTrade1.Should().Be(enrichedTrade2);
        enrichedTrade1.Equals(enrichedTrade2).Should().BeTrue();
        (enrichedTrade1 == enrichedTrade2).Should().BeTrue();
        (enrichedTrade1 != enrichedTrade2).Should().BeFalse();
    }

    [Fact]
    public void Equality_TwoEnrichedTradesWithDifferentDates_ShouldNotBeEqual()
    {
        // Arrange
        var productName = "Treasury Bill";
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        var enrichedTrade1 = EnrichedTrade.Create(TradeDate.Create("20250605"), productName, currency, price);
        var enrichedTrade2 = EnrichedTrade.Create(TradeDate.Create("20250606"), productName, currency, price);

        // Act & Assert
        enrichedTrade1.Should().NotBe(enrichedTrade2);
        enrichedTrade1.Equals(enrichedTrade2).Should().BeFalse();
        (enrichedTrade1 == enrichedTrade2).Should().BeFalse();
        (enrichedTrade1 != enrichedTrade2).Should().BeTrue();
    }

    [Fact]
    public void Equality_TwoEnrichedTradesWithDifferentProductNames_ShouldNotBeEqual()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        var enrichedTrade1 = EnrichedTrade.Create(date, "Treasury Bill", currency, price);
        var enrichedTrade2 = EnrichedTrade.Create(date, "Corporate Bond", currency, price);

        // Act & Assert
        enrichedTrade1.Should().NotBe(enrichedTrade2);
        enrichedTrade1.Equals(enrichedTrade2).Should().BeFalse();
    }

    [Fact]
    public void Equality_TwoEnrichedTradesWithDifferentCurrencies_ShouldNotBeEqual()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Treasury Bill";
        var price = Price.Create(99.99m);

        var enrichedTrade1 = EnrichedTrade.Create(date, productName, Currency.Create("USD"), price);
        var enrichedTrade2 = EnrichedTrade.Create(date, productName, Currency.Create("EUR"), price);

        // Act & Assert
        enrichedTrade1.Should().NotBe(enrichedTrade2);
        enrichedTrade1.Equals(enrichedTrade2).Should().BeFalse();
    }

    [Fact]
    public void Equality_TwoEnrichedTradesWithDifferentPrices_ShouldNotBeEqual()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Treasury Bill";
        var currency = Currency.Create("USD");

        var enrichedTrade1 = EnrichedTrade.Create(date, productName, currency, Price.Create(99.99m));
        var enrichedTrade2 = EnrichedTrade.Create(date, productName, currency, Price.Create(100.00m));

        // Act & Assert
        enrichedTrade1.Should().NotBe(enrichedTrade2);
        enrichedTrade1.Equals(enrichedTrade2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_TwoEnrichedTradesWithSameValues_ShouldHaveSameHashCode()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Treasury Bill";
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        var enrichedTrade1 = EnrichedTrade.Create(date, productName, currency, price);
        var enrichedTrade2 = EnrichedTrade.Create(date, productName, currency, price);

        // Act & Assert
        enrichedTrade1.GetHashCode().Should().Be(enrichedTrade2.GetHashCode());
    }

    #endregion

    #region Multiple EnrichedTrades Test

    [Fact]
    public void Create_MultipleEnrichedTradesWithDifferentData_ShouldSucceed()
    {
        // Arrange & Act
        var enrichedTrade1 = EnrichedTrade.Create(
            TradeDate.Create("20250605"),
            "Treasury Bill",
            Currency.Create("USD"),
            Price.Create(100.00m)
        );

        var enrichedTrade2 = EnrichedTrade.Create(
            TradeDate.Create("20240101"),
            "Corporate Bond",
            Currency.Create("EUR"),
            Price.Create(0.01m)
        );

        var enrichedTrade3 = EnrichedTrade.Create(
            TradeDate.Create("20240229"),
            string.Empty,
            Currency.Create("GBP"),
            Price.Create(12345.67m)
        );

        // Assert
        enrichedTrade1.Should().NotBeNull();
        enrichedTrade2.Should().NotBeNull();
        enrichedTrade3.Should().NotBeNull();
        enrichedTrade1.Should().NotBe(enrichedTrade2);
        enrichedTrade2.Should().NotBe(enrichedTrade3);
        enrichedTrade1.Should().NotBe(enrichedTrade3);
    }

    #endregion

    #region Edge Cases

    [Theory]
    [InlineData("")]
    [InlineData("Missing Product Name")]
    [InlineData("Treasury Bill")]
    [InlineData("A very long product name with special characters !@#$%^&*()")]
    public void Create_WithVariousProductNames_ShouldSucceed(string productName)
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        // Act
        var enrichedTrade = EnrichedTrade.Create(date, productName, currency, price);

        // Assert
        enrichedTrade.Should().NotBeNull();
        enrichedTrade.ProductName.Should().Be(productName);
    }

    [Fact]
    public void Create_WithZeroPrice_ShouldSucceed()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Zero Price Product";
        var currency = Currency.Create("USD");
        var price = Price.Create(0m);

        // Act
        var enrichedTrade = EnrichedTrade.Create(date, productName, currency, price);

        // Assert
        enrichedTrade.Should().NotBeNull();
        enrichedTrade.Price.Value.Should().Be(0m);
    }

    [Fact]
    public void Create_WithVeryLargePrice_ShouldSucceed()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productName = "Expensive Product";
        var currency = Currency.Create("USD");
        var price = Price.Create(999999999.99m);

        // Act
        var enrichedTrade = EnrichedTrade.Create(date, productName, currency, price);

        // Assert
        enrichedTrade.Should().NotBeNull();
        enrichedTrade.Price.Value.Should().Be(999999999.99m);
    }

    #endregion
}
