using System.Globalization;
using System.Text;
using Application.DTOs;

namespace Api.Tests.Helpers;

/// <summary>
/// Test data builder for creating test DTOs and CSV content.
/// Provides fluent API for constructing valid and invalid test data.
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a valid TradeInputDto with default or provided values.
    /// </summary>
    /// <param name="date">Trade date in yyyyMMdd format. Defaults to "20260111".</param>
    /// <param name="productId">Product identifier. Defaults to "1".</param>
    /// <param name="currency">Currency code. Defaults to "USD".</param>
    /// <param name="price">Trade price. Defaults to "100.50".</param>
    /// <returns>A valid TradeInputDto instance.</returns>
    public static TradeInputDto CreateValidTradeInput(
        string date = "20260111",
        string productId = "1",
        string currency = "USD",
        string price = "100.50")
    {
        return new TradeInputDto
        {
            Date = date,
            ProductId = productId,
            Currency = currency,
            Price = price
        };
    }

    /// <summary>
    /// Creates an EnrichedTradeOutputDto with default or provided values.
    /// </summary>
    /// <param name="date">Trade date in yyyyMMdd format. Defaults to "20260111".</param>
    /// <param name="productName">Product name (enriched). Defaults to "Treasury Bills Domestic".</param>
    /// <param name="currency">Currency code. Defaults to "USD".</param>
    /// <param name="price">Trade price. Defaults to "100.50".</param>
    /// <returns>An EnrichedTradeOutputDto instance.</returns>
    public static EnrichedTradeOutputDto CreateEnrichedTradeOutput(
        string date = "20260111",
        string productName = "Treasury Bills Domestic",
        string currency = "USD",
        string price = "100.50")
    {
        return new EnrichedTradeOutputDto
        {
            Date = date,
            ProductName = productName,
            Currency = currency,
            Price = price
        };
    }

    /// <summary>
    /// Creates CSV content from an array of TradeInputDto objects.
    /// Includes CSV header row.
    /// </summary>
    /// <param name="trades">Array of trade input DTOs to convert to CSV.</param>
    /// <returns>CSV-formatted string with header and data rows.</returns>
    public static string CreateCsvContent(params TradeInputDto[] trades)
    {
        var csv = new StringBuilder();

        // Write header
        csv.AppendLine("date,product_id,currency,price");

        // Write data rows
        foreach (var trade in trades)
        {
            csv.AppendLine($"{trade.Date},{trade.ProductId},{trade.Currency},{trade.Price}");
        }

        return csv.ToString();
    }

    /// <summary>
    /// Creates CSV content with a custom header row.
    /// Useful for testing invalid headers or missing fields.
    /// </summary>
    /// <param name="header">Custom CSV header row.</param>
    /// <param name="trades">Array of trade input DTOs to convert to CSV.</param>
    /// <returns>CSV-formatted string with custom header and data rows.</returns>
    public static string CreateCsvContentWithCustomHeader(
        string header,
        params TradeInputDto[] trades)
    {
        var csv = new StringBuilder();

        // Write custom header
        csv.AppendLine(header);

        // Write data rows (assuming the standard 4 fields)
        foreach (var trade in trades)
        {
            csv.AppendLine($"{trade.Date},{trade.ProductId},{trade.Currency},{trade.Price}");
        }

        return csv.ToString();
    }

    /// <summary>
    /// Creates an empty CSV file with only a header row.
    /// Useful for testing edge cases with no data.
    /// </summary>
    /// <returns>CSV-formatted string with only the header row.</returns>
    public static string CreateEmptyCsvContent()
    {
        return "date,product_id,currency,price\n";
    }

    /// <summary>
    /// Creates CSV content with malformed rows for testing validation.
    /// </summary>
    /// <param name="rows">Array of raw CSV row strings (without header).</param>
    /// <returns>CSV-formatted string with header and provided rows.</returns>
    public static string CreateCsvContentWithRawRows(params string[] rows)
    {
        var csv = new StringBuilder();
        csv.AppendLine("date,product_id,currency,price");

        foreach (var row in rows)
        {
            csv.AppendLine(row);
        }

        return csv.ToString();
    }

    /// <summary>
    /// Creates a batch of valid trade inputs for bulk testing.
    /// </summary>
    /// <param name="count">Number of trades to create.</param>
    /// <param name="startDate">Starting date in yyyyMMdd format. Defaults to "20260101".</param>
    /// <param name="startProductId">Starting product ID. Defaults to 1.</param>
    /// <returns>Array of valid TradeInputDto instances.</returns>
    public static TradeInputDto[] CreateValidTradeBatch(
        int count,
        string startDate = "20260101",
        int startProductId = 1)
    {
        var trades = new TradeInputDto[count];

        for (int i = 0; i < count; i++)
        {
            trades[i] = CreateValidTradeInput(
                date: startDate,
                productId: (startProductId + i).ToString(),
                currency: i % 2 == 0 ? "USD" : "EUR",
                price: (100.0m + i * 10).ToString("F2", CultureInfo.InvariantCulture)
            );
        }

        return trades;
    }
}
