using System.Text;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Logging;
using nietras.SeparatedValues;

namespace Api.Formatters;

/// <summary>
/// Custom input formatter for parsing CSV data into TradeInputDto collections.
/// Supports text/csv and application/csv content types with UTF-8 and Unicode encodings.
/// </summary>
public sealed partial class CsvInputFormatter : TextInputFormatter
{
    private readonly ILogger<CsvInputFormatter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="CsvInputFormatter"/> class.
    /// </summary>
    /// <param name="logger">Logger instance for diagnostic logging.</param>
    public CsvInputFormatter(ILogger<CsvInputFormatter> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);

        _logger = logger;

        // Register supported media types
        SupportedMediaTypes.Add("text/csv");
        SupportedMediaTypes.Add("application/csv");

        // Register supported encodings
        SupportedEncodings.Add(Encoding.UTF8);
        SupportedEncodings.Add(Encoding.Unicode);
    }

    /// <summary>
    /// Determines whether this formatter can read the specified type.
    /// Supports IEnumerable, IReadOnlyList, and List of TradeInputDto.
    /// </summary>
    /// <param name="context">The input formatter context.</param>
    /// <returns>True if the type can be read; otherwise, false.</returns>
    protected override bool CanReadType(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        // Support IEnumerable<TradeInputDto>, IReadOnlyList<TradeInputDto>, and List<TradeInputDto>
        return type == typeof(IEnumerable<TradeInputDto>) ||
               type == typeof(IReadOnlyList<TradeInputDto>) ||
               type == typeof(List<TradeInputDto>);
    }

    /// <summary>
    /// Reads the CSV request body and parses it into a collection of TradeInputDto objects.
    /// Uses the Sep library for high-performance CSV parsing with proper unescaping.
    /// </summary>
    /// <param name="context">The input formatter context.</param>
    /// <param name="encoding">The character encoding to use.</param>
    /// <returns>An InputFormatterResult containing the parsed trades or failure information.</returns>
    /// <remarks>
    /// Expected input format: date,product_id,currency,price (or productId instead of product_id).
    /// Quoted fields are automatically unescaped per RFC 4180.
    /// Returns failure result for empty body, missing columns, or malformed CSV.
    /// </remarks>
    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context,
        Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(encoding);

        HttpContext httpContext = context.HttpContext;
        HttpRequest request = httpContext.Request;

        // Check for empty body using the ContentLength header
        if (request.ContentLength == 0)
        {
            LogEmptyRequestBody();
            return await InputFormatterResult.FailureAsync();
        }

        try
        {
            // Read request body into a MemoryStream for Sep library compatibility
            // Sep requires a seekable stream for proper CSV parsing
            using var memoryStream = new MemoryStream();
            await request.Body.CopyToAsync(memoryStream);

            // Check for empty content
            if (memoryStream.Length == 0)
            {
                LogEmptyOrWhitespaceBody();
                return await InputFormatterResult.FailureAsync();
            }

            memoryStream.Position = 0;
            using var reader = new StreamReader(memoryStream, encoding, leaveOpen: true);

            // Parse CSV using Sep library
            var trades = new List<TradeInputDto>();

            using SepReader sepReader = Sep.Reader(o => o with { Unescape = true }).From(reader);

            // Check for empty CSV (no header row)
            if (sepReader.Header.ColNames.Count == 0)
            {
                LogEmptyOrWhitespaceBody();
                return await InputFormatterResult.FailureAsync();
            }

            // Validate required columns exist
            if (!ValidateRequiredColumns(sepReader, context))
            {
                return await InputFormatterResult.FailureAsync();
            }

            // Parse each row - streaming, one row at a time
            foreach (SepReader.Row row in sepReader)
            {
                try
                {
                    var trade = new TradeInputDto
                    {
                        Date = GetColumnValue(row, sepReader, "date"),
                        ProductId = GetColumnValue(row, sepReader, "product_id", "productId"),
                        Currency = GetColumnValue(row, sepReader, "currency"),
                        Price = GetColumnValue(row, sepReader, "price")
                    };

                    trades.Add(trade);
                }
                catch (Exception ex)
                {
                    LogRowParsingError(row.RowIndex, ex);
                    context.ModelState.AddModelError(
                        string.Empty,
                        $"Malformed CSV at row {row.RowIndex}: {ex.Message}");
                    return await InputFormatterResult.FailureAsync();
                }
            }

            LogParsingSuccess(trades.Count);
            return await InputFormatterResult.SuccessAsync(trades);
        }
        catch (Exception ex)
        {
            LogCsvParsingFailed(ex);
            context.ModelState.AddModelError(
                string.Empty,
                $"Failed to parse CSV: {ex.Message}");
            return await InputFormatterResult.FailureAsync();
        }
    }

    /// <summary>
    /// Validates that all required columns exist in the CSV header.
    /// </summary>
    /// <param name="reader">The Sep reader instance.</param>
    /// <param name="context">The input formatter context for adding errors.</param>
    /// <returns>True if all required columns exist; otherwise, false.</returns>
    private bool ValidateRequiredColumns(SepReader reader, InputFormatterContext context)
    {
        string[] requiredColumns = ["date", "currency", "price"];
        List<string> missingColumns =
        [
            ..requiredColumns.Where(column => !reader.Header.ColNames.Contains(column))
        ];

        // Check for product_id or productId (both are acceptable)
        if (!reader.Header.ColNames.Contains("product_id") &&
            !reader.Header.ColNames.Contains("productId"))
        {
            missingColumns.Add("product_id");
        }

        if (missingColumns.Count > 0)
        {
            string columnList = string.Join(", ", missingColumns);
            LogMissingColumns(columnList);
            context.ModelState.AddModelError(string.Empty, $"Missing required CSV columns: {columnList}");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets the value of a column from a CSV row, trying multiple column name variations.
    /// Uses Span to properly handle quoted and unquoted fields.
    /// </summary>
    /// <param name="row">The CSV row.</param>
    /// <param name="reader">The Sep reader containing header information.</param>
    /// <param name="primaryColumnName">The primary column name to try.</param>
    /// <param name="alternativeColumnName">An alternative column name to try if primary doesn't exist.</param>
    /// <returns>The column value as a string.</returns>
    private static string GetColumnValue(
        SepReader.Row row,
        SepReader reader,
        string primaryColumnName,
        string? alternativeColumnName = null)
    {
        // Try primary column name first
        if (reader.Header.ColNames.Contains(primaryColumnName))
        {
            // Use Span.ToString() to get the properly unquoted value
            return row[primaryColumnName].Span.ToString();
        }

        // Try an alternative column name if provided
        if (alternativeColumnName != null &&
            reader.Header.ColNames.Contains(alternativeColumnName))
        {
            // Use Span.ToString() to get the properly unquoted value
            return row[alternativeColumnName].Span.ToString();
        }

        // This should not happen if ValidateRequiredColumns is called first
        throw new InvalidOperationException($"Column '{primaryColumnName}' not found");
    }

    #region Source-Generated Logging

    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Empty request body received")]
    private partial void LogEmptyRequestBody();

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Warning,
        Message = "Empty or whitespace-only request body received")]
    private partial void LogEmptyOrWhitespaceBody();

    [LoggerMessage(
        EventId = 1003,
        Level = LogLevel.Error,
        Message = "Error parsing CSV row {RowIndex}")]
    private partial void LogRowParsingError(int rowIndex, Exception ex);

    [LoggerMessage(
        EventId = 1005,
        Level = LogLevel.Information,
        Message = "Successfully parsed {Count} trades from CSV")]
    private partial void LogParsingSuccess(int count);

    [LoggerMessage(
        EventId = 1006,
        Level = LogLevel.Error,
        Message = "Failed to parse CSV request body")]
    private partial void LogCsvParsingFailed(Exception ex);

    [LoggerMessage(
        EventId = 1007,
        Level = LogLevel.Warning,
        Message = "Missing required CSV columns: {Columns}")]
    private partial void LogMissingColumns(string columns);

    #endregion
}
