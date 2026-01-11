using FluentAssertions;
using Domain.Entities;
using Domain.ValueObjects;

namespace Domain.Tests.Entities;

public class TradeTests
{
    #region Create - Valid Data Tests

    [Fact]
    public void Create_WithValidValueObjects_ShouldSucceed()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productId = ProductIdentifier.Create(123);
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        // Act
        var trade = Trade.Create(date, productId, currency, price);

        // Assert
        trade.Should().NotBeNull();
        trade.Date.Should().Be(date);
        trade.ProductId.Should().Be(productId);
        trade.Currency.Should().Be(currency);
        trade.Price.Should().Be(price);
    }

    #endregion

    #region Create - Null Argument Tests

    [Fact]
    public void Create_WithNullDate_ShouldThrowArgumentNullException()
    {
        // Arrange
        var productId = ProductIdentifier.Create(123);
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        // Act
        var act = () => Trade.Create(null!, productId, currency, price);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("date");
    }

    [Fact]
    public void Create_WithNullProductId_ShouldThrowArgumentNullException()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        // Act
        var act = () => Trade.Create(date, null!, currency, price);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("productId");
    }

    [Fact]
    public void Create_WithNullCurrency_ShouldThrowArgumentNullException()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productId = ProductIdentifier.Create(123);
        var price = Price.Create(99.99m);

        // Act
        var act = () => Trade.Create(date, productId, null!, price);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("currency");
    }

    [Fact]
    public void Create_WithNullPrice_ShouldThrowArgumentNullException()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productId = ProductIdentifier.Create(123);
        var currency = Currency.Create("USD");

        // Act
        var act = () => Trade.Create(date, productId, currency, null!);

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
        var productId = ProductIdentifier.Create(456);
        var currency = Currency.Create("EUR");
        var price = Price.Create(150.50m);

        // Act
        var trade = Trade.Create(expectedDate, productId, currency, price);

        // Assert
        trade.Date.Should().Be(expectedDate);
        trade.Date.Value.Year.Should().Be(2025);
        trade.Date.Value.Month.Should().Be(6);
        trade.Date.Value.Day.Should().Be(15);
    }

    [Fact]
    public void ProductId_ShouldReturnCorrectValue()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var expectedProductId = ProductIdentifier.Create(789);
        var currency = Currency.Create("GBP");
        var price = Price.Create(200.00m);

        // Act
        var trade = Trade.Create(date, expectedProductId, currency, price);

        // Assert
        trade.ProductId.Should().Be(expectedProductId);
        trade.ProductId.Value.Should().Be(789);
    }

    [Fact]
    public void Currency_ShouldReturnCorrectValue()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productId = ProductIdentifier.Create(123);
        var expectedCurrency = Currency.Create("JPY");
        var price = Price.Create(10000m);

        // Act
        var trade = Trade.Create(date, productId, expectedCurrency, price);

        // Assert
        trade.Currency.Should().Be(expectedCurrency);
        trade.Currency.Value.Should().Be("JPY");
    }

    [Fact]
    public void Price_ShouldReturnCorrectValue()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productId = ProductIdentifier.Create(123);
        var currency = Currency.Create("USD");
        var expectedPrice = Price.Create(500.75m);

        // Act
        var trade = Trade.Create(date, productId, currency, expectedPrice);

        // Assert
        trade.Price.Should().Be(expectedPrice);
        trade.Price.Value.Should().Be(500.75m);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_TwoTradesWithSameValues_ShouldBeEqual()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productId = ProductIdentifier.Create(123);
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        var trade1 = Trade.Create(date, productId, currency, price);
        var trade2 = Trade.Create(date, productId, currency, price);

        // Act & Assert
        trade1.Should().Be(trade2);
        trade1.Equals(trade2).Should().BeTrue();
        (trade1 == trade2).Should().BeTrue();
        (trade1 != trade2).Should().BeFalse();
    }

    [Fact]
    public void Equality_TwoTradesWithDifferentDates_ShouldNotBeEqual()
    {
        // Arrange
        var productId = ProductIdentifier.Create(123);
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        var trade1 = Trade.Create(TradeDate.Create("20250605"), productId, currency, price);
        var trade2 = Trade.Create(TradeDate.Create("20250606"), productId, currency, price);

        // Act & Assert
        trade1.Should().NotBe(trade2);
        trade1.Equals(trade2).Should().BeFalse();
        (trade1 == trade2).Should().BeFalse();
        (trade1 != trade2).Should().BeTrue();
    }

    [Fact]
    public void Equality_TwoTradesWithDifferentProductIds_ShouldNotBeEqual()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        var trade1 = Trade.Create(date, ProductIdentifier.Create(123), currency, price);
        var trade2 = Trade.Create(date, ProductIdentifier.Create(456), currency, price);

        // Act & Assert
        trade1.Should().NotBe(trade2);
        trade1.Equals(trade2).Should().BeFalse();
    }

    [Fact]
    public void Equality_TwoTradesWithDifferentCurrencies_ShouldNotBeEqual()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productId = ProductIdentifier.Create(123);
        var price = Price.Create(99.99m);

        var trade1 = Trade.Create(date, productId, Currency.Create("USD"), price);
        var trade2 = Trade.Create(date, productId, Currency.Create("EUR"), price);

        // Act & Assert
        trade1.Should().NotBe(trade2);
        trade1.Equals(trade2).Should().BeFalse();
    }

    [Fact]
    public void Equality_TwoTradesWithDifferentPrices_ShouldNotBeEqual()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productId = ProductIdentifier.Create(123);
        var currency = Currency.Create("USD");

        var trade1 = Trade.Create(date, productId, currency, Price.Create(99.99m));
        var trade2 = Trade.Create(date, productId, currency, Price.Create(100.00m));

        // Act & Assert
        trade1.Should().NotBe(trade2);
        trade1.Equals(trade2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_TwoTradesWithSameValues_ShouldHaveSameHashCode()
    {
        // Arrange
        var date = TradeDate.Create("20250605");
        var productId = ProductIdentifier.Create(123);
        var currency = Currency.Create("USD");
        var price = Price.Create(99.99m);

        var trade1 = Trade.Create(date, productId, currency, price);
        var trade2 = Trade.Create(date, productId, currency, price);

        // Act & Assert
        trade1.GetHashCode().Should().Be(trade2.GetHashCode());
    }

    #endregion

    #region Multiple Trades Test

    [Fact]
    public void Create_MultipleTradesWithDifferentData_ShouldSucceed()
    {
        // Arrange & Act
        var trade1 = Trade.Create(
            TradeDate.Create("20250605"),
            ProductIdentifier.Create(1),
            Currency.Create("USD"),
            Price.Create(100.00m)
        );

        var trade2 = Trade.Create(
            TradeDate.Create("20240101"),
            ProductIdentifier.Create(999),
            Currency.Create("EUR"),
            Price.Create(0.01m)
        );

        var trade3 = Trade.Create(
            TradeDate.Create("20240229"),
            ProductIdentifier.Create(50),
            Currency.Create("GBP"),
            Price.Create(12345.67m)
        );

        // Assert
        trade1.Should().NotBeNull();
        trade2.Should().NotBeNull();
        trade3.Should().NotBeNull();
        trade1.Should().NotBe(trade2);
        trade2.Should().NotBe(trade3);
        trade1.Should().NotBe(trade3);
    }

    #endregion
}
