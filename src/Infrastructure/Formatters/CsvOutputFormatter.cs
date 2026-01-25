using System.Globalization;
using System.Text;
using Application.DTOs;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Formatters;

namespace Infrastructure.Formatters;

/// <summary>
/// Output formatter for writing EnrichedTradeOutputDto collections as CSV using CsvHelper.
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

        Type? type = context.ObjectType;

        return type != null && type.IsAssignableTo(typeof(IEnumerable<EnrichedTradeOutputDto>));
    }

    /// <summary>
    /// Writes the enriched trade data to the response body as CSV.
    /// Uses CsvHelper for high-performance CSV generation with proper RFC 4180 escaping.
    /// </summary>
    /// <param name="context">The formatter context containing the data to write.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <remarks>
    /// Output format: date,productName,currency,price with comma separator.
    /// Special characters in field values are automatically escaped per RFC 4180.
    /// Streams directly to response body without buffering.
    /// </remarks>
    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        HttpContext httpContext = context.HttpContext;
        var trades = (IEnumerable<EnrichedTradeOutputDto>)context.Object!;

        // Set the Content-Type header
        httpContext.Response.ContentType = "text/csv";

        // Use the writer factory from the context to create a TextWriter
        Encoding encoding = Encoding.UTF8;
        TextWriter writer = context.WriterFactory(httpContext.Response.Body, encoding);

        // Configure CsvHelper for RFC 4180 compliance with no whitespace trimming
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.None
        };

        await using var csv = new CsvWriter(writer, config);

        // Write header row
        csv.WriteField("date");
        csv.WriteField("productName");
        csv.WriteField("currency");
        csv.WriteField("price");
        await csv.NextRecordAsync();

        // Write all data rows
        foreach (var trade in trades)
        {
            csv.WriteField(trade.Date);
            csv.WriteField(trade.ProductName);
            csv.WriteField(trade.Currency);
            csv.WriteField(trade.Price);
            await csv.NextRecordAsync();
        }

        await writer.FlushAsync();
    }
}
