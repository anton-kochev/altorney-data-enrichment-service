using FluentAssertions;
using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class TradeDateTests
{
    #region Create - Valid Date Tests

    [Theory]
    [InlineData("20250605", 2025, 6, 5)]
    [InlineData("20241231", 2024, 12, 31)]
    [InlineData("20200101", 2020, 1, 1)]
    [InlineData("19900228", 1990, 2, 28)]
    [InlineData("20240229", 2024, 2, 29)] // Leap year
    public void Create_WithValidDate_ShouldSucceed(string input, int expectedYear, int expectedMonth, int expectedDay)
    {
        // Act
        var tradeDate = TradeDate.Create(input);

        // Assert
        tradeDate.Should().NotBeNull();
        tradeDate.Value.Year.Should().Be(expectedYear);
        tradeDate.Value.Month.Should().Be(expectedMonth);
        tradeDate.Value.Day.Should().Be(expectedDay);
    }

    #endregion

    #region Create - Invalid Format Tests

    [Theory]
    [InlineData("2025-06-05")] // Dashes instead of plain
    [InlineData("06052025")] // MM/DD/YYYY format
    [InlineData("250605")] // YY/MM/DD format (2 digit year)
    [InlineData("05062025")] // DD/MM/YYYY format
    [InlineData("2025/06/05")] // Slashes
    [InlineData("20250605 ")] // Trailing space
    [InlineData(" 20250605")] // Leading space
    [InlineData("202506")] // Too short (missing day)
    [InlineData("2025060")] // Too short by one digit
    [InlineData("202506055")] // Too long by one digit
    public void Create_WithInvalidFormat_ShouldThrowArgumentException(string input)
    {
        // Act
        var act = () => TradeDate.Create(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*yyyyMMdd*");
    }

    #endregion

    #region Create - Invalid Date Tests

    [Theory]
    [InlineData("20250231")] // February 31st doesn't exist
    [InlineData("20250431")] // April 31st doesn't exist
    [InlineData("20230229")] // Feb 29 in non-leap year
    [InlineData("20251301")] // Month 13
    [InlineData("20250001")] // Month 0
    [InlineData("20250100")] // Day 0
    [InlineData("20250132")] // January 32nd
    [InlineData("20251332")] // Invalid month and day
    [InlineData("00000000")] // All zeros
    public void Create_WithInvalidDate_ShouldThrowArgumentException(string input)
    {
        // Act
        var act = () => TradeDate.Create(input);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*valid*");
    }

    #endregion

    #region Create - Null/Empty Tests

    [Fact]
    public void Create_WithEmptyString_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TradeDate.Create(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    [Fact]
    public void Create_WithNull_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => TradeDate.Create(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("dateString");
    }

    [Fact]
    public void Create_WithWhitespace_ShouldThrowArgumentException()
    {
        // Act
        var act = () => TradeDate.Create("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*cannot be null or empty*");
    }

    #endregion

    #region FormattedValue Tests

    [Theory]
    [InlineData("20250605", "20250605")]
    [InlineData("20241231", "20241231")]
    [InlineData("20200101", "20200101")]
    public void FormattedValue_ShouldReturnYyyyMMddFormat(string input, string expectedOutput)
    {
        // Arrange
        var tradeDate = TradeDate.Create(input);

        // Act
        var formatted = tradeDate.FormattedValue;

        // Assert
        formatted.Should().Be(expectedOutput);
    }

    #endregion

    #region Value Tests

    [Fact]
    public void Value_ShouldReturnDateOnlyCorrectly()
    {
        // Arrange
        var input = "20250615";
        var expectedDate = new DateOnly(2025, 6, 15);

        // Act
        var tradeDate = TradeDate.Create(input);

        // Assert
        tradeDate.Value.Should().Be(expectedDate);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_TwoTradeDatesWithSameValue_ShouldBeEqual()
    {
        // Arrange
        var date1 = TradeDate.Create("20250605");
        var date2 = TradeDate.Create("20250605");

        // Act & Assert
        date1.Should().Be(date2);
        date1.Equals(date2).Should().BeTrue();
        (date1 == date2).Should().BeTrue();
        (date1 != date2).Should().BeFalse();
    }

    [Fact]
    public void Equality_TwoTradeDatesWithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var date1 = TradeDate.Create("20250605");
        var date2 = TradeDate.Create("20250606");

        // Act & Assert
        date1.Should().NotBe(date2);
        date1.Equals(date2).Should().BeFalse();
        (date1 == date2).Should().BeFalse();
        (date1 != date2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_TwoTradeDatesWithSameValue_ShouldHaveSameHashCode()
    {
        // Arrange
        var date1 = TradeDate.Create("20250605");
        var date2 = TradeDate.Create("20250605");

        // Act & Assert
        date1.GetHashCode().Should().Be(date2.GetHashCode());
    }

    #endregion
}
