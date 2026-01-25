using System.Text;
using Infrastructure.Formatters;
using Infrastructure.Tests.Helpers;
using Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Infrastructure.Tests.Formatters;

/// <summary>
/// Tests for CsvOutputFormatter.
/// Validates CSV output formatting for EnrichedTradeOutputDto collections.
/// </summary>
public sealed class CsvOutputFormatterTests
{
    private readonly CsvOutputFormatter _sut;

    public CsvOutputFormatterTests()
    {
        _sut = new CsvOutputFormatter();
    }

    #region Content-Type Tests

    [Fact]
    public void CanWriteResult_WithEnrichedTradeEnumerable_ReturnsTrue()
    {
        // Arrange
        var enrichedTrades = new List<EnrichedTradeOutputDto>
        {
            TestDataBuilder.CreateEnrichedTradeOutput()
        };

        var context = CreateOutputFormatterContext(enrichedTrades);

        // Act
        var result = _sut.CanWriteResult(context);

        // Assert
        result.Should().BeTrue("formatter should handle IEnumerable<EnrichedTradeOutputDto>");
    }

    [Fact]
    public void CanWriteResult_WithListOfEnrichedTrades_ReturnsTrue()
    {
        // Arrange
        var enrichedTrades = new List<EnrichedTradeOutputDto>
        {
            TestDataBuilder.CreateEnrichedTradeOutput()
        };

        var context = CreateOutputFormatterContext(enrichedTrades as List<EnrichedTradeOutputDto>);

        // Act
        var result = _sut.CanWriteResult(context);

        // Assert
        result.Should().BeTrue("formatter should handle List<EnrichedTradeOutputDto>");
    }

    [Fact]
    public void CanWriteResult_WithOtherType_ReturnsFalse()
    {
        // Arrange
        var someOtherObject = new { Name = "Test", Value = 123 };
        var context = CreateOutputFormatterContext(someOtherObject);

        // Act
        var result = _sut.CanWriteResult(context);

        // Assert
        result.Should().BeFalse("formatter should only handle EnrichedTradeOutputDto collections");
    }

    [Fact]
    public void CanWriteResult_WithString_ReturnsFalse()
    {
        // Arrange
        var stringObject = "Some random string";
        var context = CreateOutputFormatterContext(stringObject);

        // Act
        var result = _sut.CanWriteResult(context);

        // Assert
        result.Should().BeFalse("formatter should not handle string types");
    }

    [Fact]
    public void CanWriteResult_WithNull_ReturnsFalse()
    {
        // Arrange
        var context = CreateOutputFormatterContext(null);

        // Act
        var result = _sut.CanWriteResult(context);

        // Assert
        result.Should().BeFalse("formatter should not handle null values");
    }

    #endregion

    #region Happy Path Tests

    [Fact]
    public async Task WriteResponseBodyAsync_WithValidData_WritesCsvHeader()
    {
        // Arrange
        var enrichedTrades = new List<EnrichedTradeOutputDto>
        {
            TestDataBuilder.CreateEnrichedTradeOutput()
        };

        var stream = new MemoryStream();
        var context = CreateOutputFormatterContext(enrichedTrades, stream);

        // Act
        await _sut.WriteResponseBodyAsync(context);

        // Assert
        stream.Position = 0;
        var csvContent = await new StreamReader(stream).ReadToEndAsync();

        csvContent.Should().StartWith("date,productName,currency,price",
            "CSV should start with header row");
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithValidData_WritesDataRows()
    {
        // Arrange
        var enrichedTrade = TestDataBuilder.CreateEnrichedTradeOutput(
            date: "20260115",
            productName: "Treasury Bills Domestic",
            currency: "USD",
            price: "100.50"
        );

        var enrichedTrades = new List<EnrichedTradeOutputDto> { enrichedTrade };
        var stream = new MemoryStream();
        var context = CreateOutputFormatterContext(enrichedTrades, stream);

        // Act
        await _sut.WriteResponseBodyAsync(context);

        // Assert
        stream.Position = 0;
        var csvContent = await new StreamReader(stream).ReadToEndAsync();

        csvContent.Should().Contain("20260115,Treasury Bills Domestic,USD,100.50",
            "CSV should contain the data row with all fields");
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithMultipleTrades_WritesAllRows()
    {
        // Arrange
        var trade1 = TestDataBuilder.CreateEnrichedTradeOutput(
            date: "20260115",
            productName: "Treasury Bills Domestic",
            currency: "USD",
            price: "100.50"
        );

        var trade2 = TestDataBuilder.CreateEnrichedTradeOutput(
            date: "20260116",
            productName: "Corporate Bonds",
            currency: "EUR",
            price: "250.75"
        );

        var trade3 = TestDataBuilder.CreateEnrichedTradeOutput(
            date: "20260117",
            productName: "Government Securities",
            currency: "GBP",
            price: "500.00"
        );

        var enrichedTrades = new List<EnrichedTradeOutputDto> { trade1, trade2, trade3 };
        var stream = new MemoryStream();
        var context = CreateOutputFormatterContext(enrichedTrades, stream);

        // Act
        await _sut.WriteResponseBodyAsync(context);

        // Assert
        stream.Position = 0;
        var csvContent = await new StreamReader(stream).ReadToEndAsync();

        csvContent.Should().Contain("20260115,Treasury Bills Domestic,USD,100.50",
            "CSV should contain first trade");
        csvContent.Should().Contain("20260116,Corporate Bonds,EUR,250.75",
            "CSV should contain second trade");
        csvContent.Should().Contain("20260117,Government Securities,GBP,500.00",
            "CSV should contain third trade");
    }

    [Fact]
    public async Task WriteResponseBodyAsync_SetsContentTypeHeader()
    {
        // Arrange
        var enrichedTrades = new List<EnrichedTradeOutputDto>
        {
            TestDataBuilder.CreateEnrichedTradeOutput()
        };

        var stream = new MemoryStream();
        var httpContext = new DefaultHttpContext();
        var context = CreateOutputFormatterContext(enrichedTrades, stream, httpContext);

        // Act
        await _sut.WriteResponseBodyAsync(context);

        // Assert
        httpContext.Response.ContentType.Should().Be("text/csv",
            "response content type should be set to text/csv");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task WriteResponseBodyAsync_WithEmptyCollection_WritesHeaderOnly()
    {
        // Arrange
        var enrichedTrades = new List<EnrichedTradeOutputDto>();
        var stream = new MemoryStream();
        var context = CreateOutputFormatterContext(enrichedTrades, stream);

        // Act
        await _sut.WriteResponseBodyAsync(context);

        // Assert
        stream.Position = 0;
        var csvContent = await new StreamReader(stream).ReadToEndAsync();

        var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1, "empty collection should produce header only");
        lines[0].Should().Be("date,productName,currency,price",
            "header should be written even with no data");
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithCommaInProductName_QuotesField()
    {
        // Arrange
        var enrichedTrade = TestDataBuilder.CreateEnrichedTradeOutput(
            productName: "Treasury Bills, Domestic"
        );

        var enrichedTrades = new List<EnrichedTradeOutputDto> { enrichedTrade };
        var stream = new MemoryStream();
        var context = CreateOutputFormatterContext(enrichedTrades, stream);

        // Act
        await _sut.WriteResponseBodyAsync(context);

        // Assert
        stream.Position = 0;
        var csvContent = await new StreamReader(stream).ReadToEndAsync();

        csvContent.Should().Contain("\"Treasury Bills, Domestic\"",
            "fields containing commas should be quoted");
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithQuoteInProductName_EscapesQuotes()
    {
        // Arrange
        var enrichedTrade = TestDataBuilder.CreateEnrichedTradeOutput(
            productName: "Treasury \"Premium\" Bills"
        );

        var enrichedTrades = new List<EnrichedTradeOutputDto> { enrichedTrade };
        var stream = new MemoryStream();
        var context = CreateOutputFormatterContext(enrichedTrades, stream);

        // Act
        await _sut.WriteResponseBodyAsync(context);

        // Assert
        stream.Position = 0;
        var csvContent = await new StreamReader(stream).ReadToEndAsync();

        csvContent.Should().Contain("\"Treasury \"\"Premium\"\" Bills\"",
            "quotes in field values should be escaped by doubling them");
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var enrichedTrade = TestDataBuilder.CreateEnrichedTradeOutput(
            productName: "Bonds & Securities\nMulti-line"
        );

        var enrichedTrades = new List<EnrichedTradeOutputDto> { enrichedTrade };
        var stream = new MemoryStream();
        var context = CreateOutputFormatterContext(enrichedTrades, stream);

        // Act
        await _sut.WriteResponseBodyAsync(context);

        // Assert
        stream.Position = 0;
        var csvContent = await new StreamReader(stream).ReadToEndAsync();

        csvContent.Should().Contain("\"Bonds & Securities\nMulti-line\"",
            "special characters like newlines should be preserved within quoted fields");
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithMissingProductPlaceholder_WritesCorrectly()
    {
        // Arrange
        var enrichedTrade = TestDataBuilder.CreateEnrichedTradeOutput(
            productName: "Missing Product Name"
        );

        var enrichedTrades = new List<EnrichedTradeOutputDto> { enrichedTrade };
        var stream = new MemoryStream();
        var context = CreateOutputFormatterContext(enrichedTrades, stream);

        // Act
        await _sut.WriteResponseBodyAsync(context);

        // Assert
        stream.Position = 0;
        var csvContent = await new StreamReader(stream).ReadToEndAsync();

        csvContent.Should().Contain("Missing Product Name",
            "placeholder text for missing products should be written correctly");
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithWhitespaceInFields_PreservesWhitespace()
    {
        // Arrange
        var enrichedTrade = TestDataBuilder.CreateEnrichedTradeOutput(
            productName: "  Padded Product  ",
            currency: " USD "
        );

        var enrichedTrades = new List<EnrichedTradeOutputDto> { enrichedTrade };
        var stream = new MemoryStream();
        var context = CreateOutputFormatterContext(enrichedTrades, stream);

        // Act
        await _sut.WriteResponseBodyAsync(context);

        // Assert
        stream.Position = 0;
        var csvContent = await new StreamReader(stream).ReadToEndAsync();

        // Sep library should preserve whitespace as-is
        csvContent.Should().Contain("  Padded Product  ",
            "whitespace in fields should be preserved");
    }

    [Fact]
    public async Task WriteResponseBodyAsync_WithLargeDataset_HandlesEfficiently()
    {
        // Arrange
        var enrichedTrades = new List<EnrichedTradeOutputDto>();
        for (int i = 0; i < 1000; i++)
        {
            enrichedTrades.Add(TestDataBuilder.CreateEnrichedTradeOutput(
                date: $"2026{(i % 12 + 1):D2}{(i % 28 + 1):D2}",
                productName: $"Product {i}",
                currency: i % 2 == 0 ? "USD" : "EUR",
                price: $"{100.00m + i:F2}"
            ));
        }

        var stream = new MemoryStream();
        var context = CreateOutputFormatterContext(enrichedTrades, stream);

        // Act
        await _sut.WriteResponseBodyAsync(context);

        // Assert
        stream.Position = 0;
        var csvContent = await new StreamReader(stream).ReadToEndAsync();

        var lines = csvContent.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(1001, "should have header + 1000 data rows");
        lines[0].Should().Be("date,productName,currency,price");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates an OutputFormatterWriteContext for testing.
    /// </summary>
    private static OutputFormatterWriteContext CreateOutputFormatterContext(
        object? objectToWrite,
        Stream? stream = null,
        HttpContext? httpContext = null)
    {
        httpContext ??= new DefaultHttpContext();
        stream ??= new MemoryStream();
        httpContext.Response.Body = stream;

        return new OutputFormatterWriteContext(
            httpContext,
            (streamWriter, encoding) => new StreamWriter(stream, encoding, leaveOpen: true),
            objectToWrite?.GetType() ?? typeof(object),
            objectToWrite);
    }

    #endregion
}
