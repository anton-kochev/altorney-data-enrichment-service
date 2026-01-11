using System.Text;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc.Formatters;
using nietras.SeparatedValues;

namespace Api.Formatters;

/// <summary>
/// Output formatter for writing EnrichedTradeOutputDto collections as CSV using the Sep library.
/// Supports text/csv media type with UTF-8 encoding.
/// </summary>
public sealed class CsvOutputFormatter : OutputFormatter
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CsvOutputFormatter"/> class.
    /// </summary>
    public CsvOutputFormatter()
    {
        SupportedMediaTypes.Add("text/csv");
    }

    /// <summary>
    /// Determines whether this formatter can write the specified type.
    /// Returns true for IEnumerable&lt;EnrichedTradeOutputDto&gt; or List&lt;EnrichedTradeOutputDto&gt;.
    /// </summary>
    /// <param name="context">The formatter context.</param>
    /// <returns>True if the type can be written; otherwise false.</returns>
    public override bool CanWriteResult(OutputFormatterCanWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var type = context.ObjectType;

        if (type == null)
        {
            return false;
        }

        return type.IsAssignableTo(typeof(IEnumerable<EnrichedTradeOutputDto>));
    }

    /// <summary>
    /// Writes the enriched trade data to the response body as CSV.
    /// Uses the Sep library for high-performance CSV generation with proper escaping.
    /// </summary>
    /// <param name="context">The formatter context containing the data to write.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <remarks>
    /// Output format: date,productName,currency,price with comma separator.
    /// Special characters in field values are automatically escaped per RFC 4180.
    /// </remarks>
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var httpContext = context.HttpContext;
        var trades = (IEnumerable<EnrichedTradeOutputDto>)context.Object!;

        // Set the Content-Type header
        httpContext.Response.ContentType = "text/csv";

        // Use the writer factory from the context to create a TextWriter
        var encoding = Encoding.UTF8;
        var writer = context.WriterFactory(httpContext.Response.Body, encoding);

        var tradesList = trades.ToList();

        // Handle empty collection - write header only
        if (tradesList.Count == 0)
        {
            await writer.WriteLineAsync("date,productName,currency,price");
            await writer.FlushAsync();
            return;
        }

        // Use Sep library to write CSV with comma separator and escape special characters
        using var sepWriter = Sep.New(',').Writer(options => options with { Escape = true }).To(writer);

        // Write all data rows
        foreach (var trade in tradesList)
        {
            using var row = sepWriter.NewRow();
            row["date"].Set(trade.Date);
            row["productName"].Set(trade.ProductName);
            row["currency"].Set(trade.Currency);
            row["price"].Set(trade.Price);
        }

        await writer.FlushAsync();
    }
}
