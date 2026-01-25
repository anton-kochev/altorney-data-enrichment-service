using Application.DTOs;
using Application.Mappers;
using FluentAssertions;

namespace Application.Tests.Mappers;

public class TradeMapperTests
{
    #region Happy Path Tests

    [Fact]
    public void TryMapToTrade_WithValidInput_ReturnsSuccessfulResult()
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = "99.99"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Trade.Should().NotBeNull();
        result.Failure.Should().BeNull();
    }

    [Fact]
    public void TryMapToTrade_WithValidInput_ReturnsCorrectTradeEntity()
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = "99.99"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.Trade!.Date.FormattedValue.Should().Be("20231215");
        result.Trade.ProductId.Value.Should().Be(123);
        result.Trade.Currency.Value.Should().Be("USD");
        result.Trade.Price.Value.Should().Be(99.99m);
    }

    [Fact]
    public void TryMapToTrade_WithValidInput_ReturnsTrimmedPrice()
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = "  99.99  "
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.TrimmedPrice.Should().Be("99.99");
    }

    [Theory]
    [InlineData("100", "100")]
    [InlineData("100.0", "100.0")]
    [InlineData("100.00", "100.00")]
    [InlineData(" 99.99 ", "99.99")]
    public void TryMapToTrade_PreservesPriceStringFormat(string inputPrice, string expectedTrimmedPrice)
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = inputPrice
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.TrimmedPrice.Should().Be(expectedTrimmedPrice);
    }

    #endregion

    #region Missing Fields Tests

    [Theory]
    [InlineData("", "123", "USD", "10.00", "date")]
    [InlineData("20231215", "", "USD", "10.00", "productId")]
    [InlineData("20231215", "123", "", "10.00", "currency")]
    [InlineData("20231215", "123", "USD", "", "price")]
    public void TryMapToTrade_WithMissingField_ReturnsFailureWithMissingFields(
        string date, string productId, string currency, string price, string expectedMissingField)
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = date,
            ProductId = productId,
            Currency = currency,
            Price = price
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Trade.Should().BeNull();
        result.Failure.Should().NotBeNull();
        result.Failure!.Type.Should().Be(TradeMapper.ValidationFailureType.MissingFields);
        result.Failure.MissingFields.Should().Contain(expectedMissingField);
    }

    [Fact]
    public void TryMapToTrade_WithMultipleMissingFields_ReturnsAllMissingFields()
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "",
            ProductId = "",
            Currency = "USD",
            Price = ""
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Failure!.MissingFields.Should().Contain("date");
        result.Failure.MissingFields.Should().Contain("productId");
        result.Failure.MissingFields.Should().Contain("price");
        result.Failure.MissingFields.Should().NotContain("currency");
    }

    [Theory]
    [InlineData("   ")]
    [InlineData("\t")]
    [InlineData("\n")]
    public void TryMapToTrade_WithWhitespaceOnlyField_TreatsAsMissing(string whitespace)
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = whitespace,
            ProductId = "123",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Failure!.Type.Should().Be(TradeMapper.ValidationFailureType.MissingFields);
        result.Failure.MissingFields.Should().Contain("date");
    }

    #endregion

    #region Invalid Date Tests

    [Theory]
    [InlineData("2023-12-15")]
    [InlineData("12/15/2023")]
    [InlineData("invalid")]
    [InlineData("2023121")]
    [InlineData("202312150")]
    public void TryMapToTrade_WithInvalidDateFormat_ReturnsDateFailure(string invalidDate)
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = invalidDate,
            ProductId = "123",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Failure!.Type.Should().Be(TradeMapper.ValidationFailureType.InvalidDateFormat);
        result.Failure.InvalidDateReason.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("20231332")]
    [InlineData("20230229")]
    [InlineData("20230431")]
    public void TryMapToTrade_WithInvalidCalendarDate_ReturnsDateFailure(string invalidDate)
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = invalidDate,
            ProductId = "123",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Failure!.Type.Should().Be(TradeMapper.ValidationFailureType.InvalidDateFormat);
    }

    #endregion

    #region Invalid ProductId Tests

    [Theory]
    [InlineData("abc")]
    [InlineData("12.5")]
    [InlineData("")]
    public void TryMapToTrade_WithNonIntegerProductId_ReturnsProductIdFailure(string invalidProductId)
    {
        // Arrange - Note: empty string caught by missing fields first
        if (string.IsNullOrEmpty(invalidProductId))
        {
            return; // Skip - caught by missing fields
        }

        var input = new TradeInputDto
        {
            Date = "20231215",
            ProductId = invalidProductId,
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Failure!.Type.Should().Be(TradeMapper.ValidationFailureType.InvalidProductId);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("-100")]
    public void TryMapToTrade_WithNonPositiveProductId_ReturnsProductIdFailure(string invalidProductId)
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "20231215",
            ProductId = invalidProductId,
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Failure!.Type.Should().Be(TradeMapper.ValidationFailureType.InvalidProductId);
    }

    #endregion

    #region Invalid Price Tests

    [Theory]
    [InlineData("abc")]
    [InlineData("not-a-number")]
    public void TryMapToTrade_WithNonNumericPrice_ReturnsPriceFailure(string invalidPrice)
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = invalidPrice
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Failure!.Type.Should().Be(TradeMapper.ValidationFailureType.InvalidPrice);
    }

    [Fact]
    public void TryMapToTrade_WithNegativePrice_ReturnsPriceFailure()
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = "-10.00"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Failure!.Type.Should().Be(TradeMapper.ValidationFailureType.InvalidPrice);
    }

    [Fact]
    public void TryMapToTrade_WithZeroPrice_Succeeds()
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "USD",
            Price = "0"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Trade!.Price.Value.Should().Be(0m);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void TryMapToTrade_WithLeapYearDate_Succeeds()
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "20240229",
            ProductId = "123",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Trade!.Date.FormattedValue.Should().Be("20240229");
    }

    [Fact]
    public void TryMapToTrade_WithWhitespaceAroundDate_TrimsAndSucceeds()
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "  20231215  ",
            ProductId = "123",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Trade!.Date.FormattedValue.Should().Be("20231215");
    }

    [Fact]
    public void TryMapToTrade_WithWhitespaceAroundCurrency_TrimsAndSucceeds()
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "20231215",
            ProductId = "123",
            Currency = "  USD  ",
            Price = "10.00"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Trade!.Currency.Value.Should().Be("USD");
    }

    [Fact]
    public void TryMapToTrade_StoresRawInputInFailure()
    {
        // Arrange
        var input = new TradeInputDto
        {
            Date = "invalid",
            ProductId = "123",
            Currency = "USD",
            Price = "10.00"
        };

        // Act
        var result = TradeMapper.TryMapToTrade(input);

        // Assert
        result.Failure!.RawInput.Should().BeSameAs(input);
    }

    #endregion
}
