using FluentAssertions;
using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class CurrencyTests
{
    #region Create - Valid Tests

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("GBP")]
    [InlineData("JPY")]
    [InlineData("CHF")]
    [InlineData("AUD")]
    [InlineData("CAD")]
    public void Create_WithValidCurrency_ShouldSucceed(string currencyCode)
    {
        // Act
        var currency = Currency.Create(currencyCode);

        // Assert
        currency.Should().NotBeNull();
        currency.Value.Should().Be(currencyCode);
    }

    [Fact]
    public void Create_WithLowercaseCurrency_ShouldSucceed()
    {
        // Act
        var currency = Currency.Create("usd");

        // Assert
        currency.Should().NotBeNull();
        currency.Value.Should().Be("usd");
    }

    [Fact]
    public void Create_WithMixedCaseCurrency_ShouldSucceed()
    {
        // Act
        var currency = Currency.Create("UsDx");

        // Assert
        currency.Should().NotBeNull();
        currency.Value.Should().Be("UsDx");
    }

    #endregion

    #region Create - Whitespace Trimming Tests

    [Theory]
    [InlineData(" USD", "USD")]
    [InlineData("USD ", "USD")]
    [InlineData(" USD ", "USD")]
    [InlineData("  EUR  ", "EUR")]
    [InlineData("\tGBP\t", "GBP")]
    public void Create_WithLeadingTrailingSpaces_ShouldTrim(string input, string expectedValue)
    {
        // Act
        var currency = Currency.Create(input);

        // Assert
        currency.Value.Should().Be(expectedValue);
    }

    #endregion

    #region Create - Null/Empty Tests

    [Fact]
    public void Create_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act
        var act = () => Currency.Create(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*")
            .WithParameterName("currencyCode");
    }

    [Fact]
    public void Create_WithNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => Currency.Create(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("currencyCode");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    [InlineData("  \t  ")]
    public void Create_WithWhitespaceOnly_ShouldThrowArgumentException(string whitespace)
    {
        // Act
        var act = () => Currency.Create(whitespace);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*")
            .WithParameterName("currencyCode");
    }

    #endregion

    #region Value Tests

    [Theory]
    [InlineData("USD")]
    [InlineData("EUR")]
    [InlineData("CustomCurrency")]
    public void Value_ShouldReturnCorrectString(string expectedValue)
    {
        // Arrange
        var currency = Currency.Create(expectedValue);

        // Act
        var actualValue = currency.Value;

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_TwoCurrenciesWithSameValue_ShouldBeEqual()
    {
        // Arrange
        var currency1 = Currency.Create("USD");
        var currency2 = Currency.Create("USD");

        // Act & Assert
        currency1.Should().Be(currency2);
        currency1.Equals(currency2).Should().BeTrue();
        (currency1 == currency2).Should().BeTrue();
        (currency1 != currency2).Should().BeFalse();
    }

    [Fact]
    public void Equality_TwoCurrenciesWithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var currency1 = Currency.Create("USD");
        var currency2 = Currency.Create("EUR");

        // Act & Assert
        currency1.Should().NotBe(currency2);
        currency1.Equals(currency2).Should().BeFalse();
        (currency1 == currency2).Should().BeFalse();
        (currency1 != currency2).Should().BeTrue();
    }

    [Fact]
    public void Equality_TwoCurrenciesWithDifferentCasing_ShouldNotBeEqual()
    {
        // Arrange
        var currency1 = Currency.Create("USD");
        var currency2 = Currency.Create("usd");

        // Act & Assert
        currency1.Should().NotBe(currency2);
        currency1.Equals(currency2).Should().BeFalse();
    }

    [Fact]
    public void GetHashCode_TwoCurrenciesWithSameValue_ShouldHaveSameHashCode()
    {
        // Arrange
        var currency1 = Currency.Create("USD");
        var currency2 = Currency.Create("USD");

        // Act & Assert
        currency1.GetHashCode().Should().Be(currency2.GetHashCode());
    }

    #endregion
}
