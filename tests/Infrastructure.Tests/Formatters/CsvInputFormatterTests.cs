using System.Text;
using Infrastructure.Tests.Helpers;
using Application.DTOs;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Logging.Testing;

namespace Infrastructure.Tests.Formatters;

/// <summary>
/// Unit tests for CsvInputFormatter following TDD approach.
/// These tests define the behavior before implementation exists.
/// </summary>
public class CsvInputFormatterTests : IDisposable
{
    private readonly FakeLogger<Infrastructure.Formatters.CsvInputFormatter> _fakeLogger;
    private readonly DefaultHttpContext _httpContext;
    private readonly ModelStateDictionary _modelState;

    public CsvInputFormatterTests()
    {
        _fakeLogger = new FakeLogger<Infrastructure.Formatters.CsvInputFormatter>();
        _httpContext = new DefaultHttpContext();
        _modelState = new ModelStateDictionary();
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Content-Type Tests

    [Fact]
    public void CanRead_WithTextCsvContentType_ReturnsTrue()
    {
        // Arrange
        var formatter = CreateFormatter();
        var context = CreateInputFormatterContext("text/csv");

        // Act
        var result = formatter.CanRead(context);

        // Assert
        result.Should().BeTrue("text/csv is a supported content type");
    }

    [Fact]
    public void CanRead_WithApplicationCsvContentType_ReturnsTrue()
    {
        // Arrange
        var formatter = CreateFormatter();
        var context = CreateInputFormatterContext("application/csv");

        // Act
        var result = formatter.CanRead(context);

        // Assert
        result.Should().BeTrue("application/csv is a supported content type");
    }

    [Fact]
    public void CanRead_WithJsonContentType_ReturnsFalse()
    {
        // Arrange
        var formatter = CreateFormatter();
        var context = CreateInputFormatterContext("application/json");

        // Act
        var result = formatter.CanRead(context);

        // Assert
        result.Should().BeFalse("application/json is not a supported content type");
    }

    [Fact]
    public void CanRead_WithTextCsvAndCharset_ReturnsTrue()
    {
        // Arrange
        var formatter = CreateFormatter();
        var context = CreateInputFormatterContext("text/csv; charset=utf-8");

        // Act
        var result = formatter.CanRead(context);

        // Assert
        result.Should().BeTrue("text/csv with charset should be supported");
    }

    #endregion

    #region Happy Path Tests

    [Fact]
    public async Task ReadRequestBodyAsync_WithValidCsv_ReturnsTradeInputDtos()
    {
        // Arrange
        var formatter = CreateFormatter();
        var trade = TestDataBuilder.CreateValidTradeInput();
        var csvContent = TestDataBuilder.CreateCsvContent(trade);
        var context = CreateInputFormatterContext("text/csv", csvContent);

        // Act
        var result = await formatter.ReadRequestBodyAsync(context);

        // Assert
        result.Should().NotBeNull();
        result.HasError.Should().BeFalse();
        result.Model.Should().BeAssignableTo<IEnumerable<TradeInputDto>>();

        var trades = (result.Model as IEnumerable<TradeInputDto>)!.ToList();
        trades.Should().HaveCount(1);
        trades[0].Date.Should().Be(trade.Date);
        trades[0].ProductId.Should().Be(trade.ProductId);
        trades[0].Currency.Should().Be(trade.Currency);
        trades[0].Price.Should().Be(trade.Price);
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithValidCsv_ParsesAllFields()
    {
        // Arrange
        var formatter = CreateFormatter();
        var expectedTrade = TestDataBuilder.CreateValidTradeInput(
            date: "20260115",
            productId: "42",
            currency: "EUR",
            price: "99.99"
        );
        var csvContent = TestDataBuilder.CreateCsvContent(expectedTrade);
        var context = CreateInputFormatterContext("text/csv", csvContent);

        // Act
        var result = await formatter.ReadRequestBodyAsync(context);

        // Assert
        result.HasError.Should().BeFalse();
        var trades = (result.Model as IEnumerable<TradeInputDto>)!.ToList();
        var actualTrade = trades.Should().ContainSingle().Which;

        actualTrade.Date.Should().Be("20260115");
        actualTrade.ProductId.Should().Be("42");
        actualTrade.Currency.Should().Be("EUR");
        actualTrade.Price.Should().Be("99.99");
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithMultipleRows_ReturnsAllRows()
    {
        // Arrange
        var formatter = CreateFormatter();
        var trades = new[]
        {
            TestDataBuilder.CreateValidTradeInput("20260101", "1", "USD", "100.50"),
            TestDataBuilder.CreateValidTradeInput("20260102", "2", "EUR", "200.75"),
            TestDataBuilder.CreateValidTradeInput("20260103", "3", "GBP", "300.25")
        };
        var csvContent = TestDataBuilder.CreateCsvContent(trades);
        var context = CreateInputFormatterContext("text/csv", csvContent);

        // Act
        var result = await formatter.ReadRequestBodyAsync(context);

        // Assert
        result.HasError.Should().BeFalse();
        var resultTrades = (result.Model as IEnumerable<TradeInputDto>)!.ToList();
        resultTrades.Should().HaveCount(3);

        resultTrades[0].ProductId.Should().Be("1");
        resultTrades[1].ProductId.Should().Be("2");
        resultTrades[2].ProductId.Should().Be("3");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task ReadRequestBodyAsync_WithExtraColumns_IgnoresExtraColumns()
    {
        // Arrange
        var formatter = CreateFormatter();
        var csvContent = "date,product_id,currency,price,extra_column\n" +
                        "20260111,1,USD,100.50,ignored_value\n";
        var context = CreateInputFormatterContext("text/csv", csvContent);

        // Act
        var result = await formatter.ReadRequestBodyAsync(context);

        // Assert
        result.HasError.Should().BeFalse();
        var trades = (result.Model as IEnumerable<TradeInputDto>)!.ToList();
        var trade = trades.Should().ContainSingle().Which;

        trade.Date.Should().Be("20260111");
        trade.ProductId.Should().Be("1");
        trade.Currency.Should().Be("USD");
        trade.Price.Should().Be("100.50");
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithWhitespaceInFields_PreservesWhitespace()
    {
        // Arrange
        var formatter = CreateFormatter();
        var csvContent = "date,product_id,currency,price\n" +
                        " 20260111 , 1 , USD , 100.50 \n";
        var context = CreateInputFormatterContext("text/csv", csvContent);

        // Act
        var result = await formatter.ReadRequestBodyAsync(context);

        // Assert
        result.HasError.Should().BeFalse();
        var trades = (result.Model as IEnumerable<TradeInputDto>)!.ToList();
        var trade = trades.Should().ContainSingle().Which;

        // CSV parser should preserve whitespace - trimming is enrichment service's responsibility
        trade.Date.Should().Be(" 20260111 ");
        trade.ProductId.Should().Be(" 1 ");
        trade.Currency.Should().Be(" USD ");
        trade.Price.Should().Be(" 100.50 ");
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithQuotedFields_UnquotesCorrectly()
    {
        // Arrange
        var formatter = CreateFormatter();
        var csvContent = "date,product_id,currency,price\n" +
                        "\"20260111\",\"1\",\"USD\",\"100.50\"\n";
        var context = CreateInputFormatterContext("text/csv", csvContent);

        // Act
        var result = await formatter.ReadRequestBodyAsync(context);

        // Assert
        result.HasError.Should().BeFalse();
        var trades = (result.Model as IEnumerable<TradeInputDto>)!.ToList();
        var trade = trades.Should().ContainSingle().Which;

        trade.Date.Should().Be("20260111");
        trade.ProductId.Should().Be("1");
        trade.Currency.Should().Be("USD");
        trade.Price.Should().Be("100.50");
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithHeaderOnly_ReturnsEmptyCollection()
    {
        // Arrange
        var formatter = CreateFormatter();
        var csvContent = TestDataBuilder.CreateEmptyCsvContent();
        var context = CreateInputFormatterContext("text/csv", csvContent);

        // Act
        var result = await formatter.ReadRequestBodyAsync(context);

        // Assert
        result.HasError.Should().BeFalse();
        var trades = (result.Model as IEnumerable<TradeInputDto>)!.ToList();
        trades.Should().BeEmpty("CSV with only header should return empty collection");
    }

    #endregion

    #region Error Cases

    [Fact]
    public async Task ReadRequestBodyAsync_WithEmptyBody_ReturnsFailure()
    {
        // Arrange
        var formatter = CreateFormatter();
        var context = CreateInputFormatterContext("text/csv", string.Empty);

        // Act
        var result = await formatter.ReadRequestBodyAsync(context);

        // Assert
        result.HasError.Should().BeTrue("empty body should result in failure");
        result.Model.Should().BeNull();
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithMissingDateColumn_ReturnsFailure()
    {
        // Arrange
        var formatter = CreateFormatter();
        var csvContent = TestDataBuilder.CreateCsvContentWithCustomHeader(
            "product_id,currency,price",
            TestDataBuilder.CreateValidTradeInput()
        );
        var context = CreateInputFormatterContext("text/csv", csvContent);

        // Act
        var result = await formatter.ReadRequestBodyAsync(context);

        // Assert
        result.HasError.Should().BeTrue("missing date column should result in failure");
        context.ModelState.Should().ContainKey(string.Empty);
        context.ModelState[string.Empty]!.Errors.Should().Contain(
            e => e.ErrorMessage.Contains("date", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithMissingProductIdColumn_ReturnsFailure()
    {
        // Arrange
        var formatter = CreateFormatter();
        var csvContent = TestDataBuilder.CreateCsvContentWithCustomHeader(
            "date,currency,price",
            TestDataBuilder.CreateValidTradeInput()
        );
        var context = CreateInputFormatterContext("text/csv", csvContent);

        // Act
        var result = await formatter.ReadRequestBodyAsync(context);

        // Assert
        result.HasError.Should().BeTrue("missing product_id column should result in failure");
        context.ModelState.Should().ContainKey(string.Empty);
        context.ModelState[string.Empty]!.Errors.Should().Contain(
            e => e.ErrorMessage.Contains("product_id", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ReadRequestBodyAsync_WithMalformedCsv_ReturnsFailure()
    {
        // Arrange
        var formatter = CreateFormatter();
        var csvContent = "date,product_id,currency,price\n" +
                        "20260111,1,USD\n" + // Missing price field
                        "20260112,2,EUR,100.50,extra\n"; // Inconsistent columns
        var context = CreateInputFormatterContext("text/csv", csvContent);

        // Act
        var result = await formatter.ReadRequestBodyAsync(context);

        // Assert
        result.HasError.Should().BeTrue("malformed CSV should result in failure");
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a CsvInputFormatter instance.
    /// </summary>
    private TextInputFormatter CreateFormatter()
    {
        return new Infrastructure.Formatters.CsvInputFormatter(_fakeLogger);
    }

    /// <summary>
    /// Creates an InputFormatterContext for testing.
    /// </summary>
    private InputFormatterContext CreateInputFormatterContext(
        string contentType,
        string? csvContent = null)
    {
        _httpContext.Request.ContentType = contentType;

        if (csvContent != null)
        {
            var bytes = Encoding.UTF8.GetBytes(csvContent);
            _httpContext.Request.Body = new MemoryStream(bytes);
            _httpContext.Request.ContentLength = bytes.Length;
        }

        return new InputFormatterContext(
            _httpContext,
            modelName: string.Empty,
            modelState: _modelState,
            metadata: new EmptyModelMetadataProvider().GetMetadataForType(typeof(IEnumerable<TradeInputDto>)),
            readerFactory: (stream, encoding) => new StreamReader(stream, encoding)
        );
    }

    #endregion
}
