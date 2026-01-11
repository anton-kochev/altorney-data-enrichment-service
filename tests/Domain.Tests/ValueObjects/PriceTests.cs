using FluentAssertions;
using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class PriceTests
{
    #region Create - Valid Positive Price Tests

    [Theory]
    [InlineData(0.01)]
    [InlineData(1.00)]
    [InlineData(10.50)]
    [InlineData(100.99)]
    [InlineData(1000.00)]
    [InlineData(999999.99)]
    public void Create_WithPositivePrice_ShouldSucceed(decimal value)
    {
        // Act
        var price = Price.Create(value);

        // Assert
        price.Should().NotBeNull();
        price.Value.Should().Be(value);
    }

    #endregion

    #region Create - Zero Price Tests

    [Fact]
    public void Create_WithZero_ShouldSucceed()
    {
        // Act
        var price = Price.Create(0m);

        // Assert
        price.Should().NotBeNull();
        price.Value.Should().Be(0m);
    }

    [Fact]
    public void Create_WithZeroDecimal_ShouldSucceed()
    {
        // Act
        var price = Price.Create(0.00m);

        // Assert
        price.Should().NotBeNull();
        price.Value.Should().Be(0.00m);
    }

    #endregion

    #region Create - Negative Price Tests

    [Theory]
    [InlineData(-0.01)]
    [InlineData(-1.00)]
    [InlineData(-100.50)]
    [InlineData(-999999.99)]
    public void Create_WithNegativePrice_ShouldThrowArgumentException(decimal value)
    {
        // Act
        var act = () => Price.Create(value);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be negative*")
            .WithParameterName("amount");
    }

    #endregion

    #region Create - Large Decimal Tests

    [Fact]
    public void Create_WithLargeDecimal_ShouldSucceed()
    {
        // Arrange
        var largeValue = 999999999999.99m;

        // Act
        var price = Price.Create(largeValue);

        // Assert
        price.Should().NotBeNull();
        price.Value.Should().Be(largeValue);
    }

    [Fact]
    public void Create_WithMaxDecimal_ShouldSucceed()
    {
        // Arrange
        var maxValue = decimal.MaxValue;

        // Act
        var price = Price.Create(maxValue);

        // Assert
        price.Should().NotBeNull();
        price.Value.Should().Be(maxValue);
    }

    #endregion

    #region Create - Decimal Precision Tests

    [Theory]
    [InlineData(10.1)]
    [InlineData(10.12)]
    [InlineData(10.123)]
    [InlineData(10.1234)]
    [InlineData(10.12345)]
    [InlineData(10.123456)]
    public void Create_WithDecimalPrecision_ShouldPreserve(decimal value)
    {
        // Act
        var price = Price.Create(value);

        // Assert
        price.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithHighPrecisionDecimal_ShouldPreserveExactValue()
    {
        // Arrange
        var preciseValue = 123.456789m;

        // Act
        var price = Price.Create(preciseValue);

        // Assert
        price.Value.Should().Be(preciseValue);
    }

    #endregion

    #region Value Tests

    [Theory]
    [InlineData(0.00)]
    [InlineData(1.50)]
    [InlineData(99.99)]
    [InlineData(1000000.00)]
    public void Value_ShouldReturnCorrectDecimal(decimal expectedValue)
    {
        // Arrange
        var price = Price.Create(expectedValue);

        // Act
        var actualValue = price.Value;

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_TwoPricesWithSameValue_ShouldBeEqual()
    {
        // Arrange
        var price1 = Price.Create(100.50m);
        var price2 = Price.Create(100.50m);

        // Act & Assert
        price1.Should().Be(price2);
        price1.Equals(price2).Should().BeTrue();
        (price1 == price2).Should().BeTrue();
        (price1 != price2).Should().BeFalse();
    }

    [Fact]
    public void Equality_TwoPricesWithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var price1 = Price.Create(100.50m);
        var price2 = Price.Create(100.51m);

        // Act & Assert
        price1.Should().NotBe(price2);
        price1.Equals(price2).Should().BeFalse();
        (price1 == price2).Should().BeFalse();
        (price1 != price2).Should().BeTrue();
    }

    [Fact]
    public void Equality_TwoPricesWithSameValueDifferentPrecision_ShouldBeEqual()
    {
        // Arrange
        var price1 = Price.Create(100.5m);
        var price2 = Price.Create(100.50m);

        // Act & Assert
        price1.Should().Be(price2);
        price1.Equals(price2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_TwoPricesWithSameValue_ShouldHaveSameHashCode()
    {
        // Arrange
        var price1 = Price.Create(100.50m);
        var price2 = Price.Create(100.50m);

        // Act & Assert
        price1.GetHashCode().Should().Be(price2.GetHashCode());
    }

    #endregion
}
