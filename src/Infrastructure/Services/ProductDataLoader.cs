using System.Globalization;
using Application.Configuration;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

public sealed partial class ProductDataLoader : IHostedService
{
    private readonly CsvProductLookupService _lookupService;
    private readonly ProductDataOptions _options;
    private readonly ILogger<ProductDataLoader> _logger;

    public ProductDataLoader(
        CsvProductLookupService lookupService,
        IOptions<ProductDataOptions> options,
        ILogger<ProductDataLoader> logger)
    {
        ArgumentNullException.ThrowIfNull(lookupService, nameof(lookupService));
        ArgumentNullException.ThrowIfNull(options, nameof(options));
        ArgumentNullException.ThrowIfNull(logger, nameof(logger));

        _lookupService = lookupService;
        _options = options.Value;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!File.Exists(_options.FilePath))
        {
            throw new FileNotFoundException($"Product data file not found at path: {_options.FilePath}", _options.FilePath);
        }

        LogLoadingProductData(_options.FilePath);

        Dictionary<int, string> products = new();
        int skippedCount = 0;

        using var streamReader = new StreamReader(_options.FilePath);

        // Configure CsvHelper for RFC 4180 compliance with no whitespace trimming
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            TrimOptions = TrimOptions.None,
            MissingFieldFound = null
        };

        using var csv = new CsvReader(streamReader, config);

        // Read header
        await csv.ReadAsync();
        csv.ReadHeader();

        int rowIndex = 1; // Start at 1 for data rows
        while (await csv.ReadAsync())
        {
            rowIndex++;
            cancellationToken.ThrowIfCancellationRequested();

            string? productIdText = csv.GetField("productId");
            string? productNameText = csv.GetField("productName");

            // Skip if productId is empty or whitespace
            if (string.IsNullOrWhiteSpace(productIdText))
            {
                LogSkippingRow(rowIndex, "Empty productId");
                skippedCount++;
                continue;
            }

            // Skip if productName is empty or whitespace
            if (string.IsNullOrWhiteSpace(productNameText))
            {
                LogSkippingRow(rowIndex, "Empty productName");
                skippedCount++;
                continue;
            }

            // Try to parse productId as integer
            if (!int.TryParse(productIdText, out var productId))
            {
                LogSkippingRow(rowIndex, "Non-integer productId");
                skippedCount++;
                continue;
            }

            // Skip if productId is zero or negative
            if (productId <= 0)
            {
                LogSkippingRow(rowIndex, "ProductId must be positive");
                skippedCount++;
                continue;
            }

            string productName = productNameText.Trim();

            // Skip duplicates (keep the first occurrence)
            if (!products.TryAdd(productId, productName))
            {
                LogDuplicateProductId(productId, rowIndex);
                skippedCount++;
            }
        }

        _lookupService.LoadProducts(products);

        LogProductDataLoaded(products.Count, skippedCount);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    [LoggerMessage(EventId = 1, Level = LogLevel.Information, Message = "Loading product data from file: {FilePath}")]
    private partial void LogLoadingProductData(string filePath);

    [LoggerMessage(EventId = 2, Level = LogLevel.Warning, Message = "Skipping row {RowIndex}: {Reason}")]
    private partial void LogSkippingRow(int rowIndex, string reason);

    [LoggerMessage(EventId = 3, Level = LogLevel.Warning, Message = "Duplicate productId {ProductId} found at row {RowIndex}, keeping first occurrence")]
    private partial void LogDuplicateProductId(int productId, int rowIndex);

    [LoggerMessage(EventId = 4, Level = LogLevel.Information, Message = "Successfully loaded {ValidCount} products (skipped {SkippedCount} invalid rows)")]
    private partial void LogProductDataLoaded(int validCount, int skippedCount);
}
