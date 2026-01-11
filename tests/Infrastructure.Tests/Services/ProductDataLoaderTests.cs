using FluentAssertions;
using Infrastructure.Services;
using Application.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using Microsoft.Extensions.Options;

namespace Infrastructure.Tests.Services;

public class ProductDataLoaderTests : IDisposable
{
    private readonly FakeLogger<ProductDataLoader> _fakeLogger;
    private readonly string _testFilePath;
    private readonly string _testDirectory;

    public ProductDataLoaderTests()
    {
        _fakeLogger = new FakeLogger<ProductDataLoader>();
        _testDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_testDirectory);
        _testFilePath = Path.Combine(_testDirectory, "products.csv");
    }

    public void Dispose()
    {
        if (Directory.Exists(_testDirectory))
        {
            Directory.Delete(_testDirectory, recursive: true);
        }
    }

    #region Constructor Tests

    [Fact]
    public void Constructor_WithNullLookupService_ShouldThrowArgumentNullException()
    {
        // Arrange
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });

        // Act
        var act = () => new ProductDataLoader(null!, options, _fakeLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("lookupService");
    }

    [Fact]
    public void Constructor_WithNullOptions_ShouldThrowArgumentNullException()
    {
        // Arrange
        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());

        // Act
        var act = () => new ProductDataLoader(lookupService, null!, _fakeLogger);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("options");
    }

    [Fact]
    public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
    {
        // Arrange
        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });

        // Act
        var act = () => new ProductDataLoader(lookupService, options, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("logger");
    }

    [Fact]
    public void Constructor_WithValidParameters_ShouldSucceed()
    {
        // Arrange
        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });

        // Act
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Assert
        loader.Should().NotBeNull();
        loader.Should().BeAssignableTo<IHostedService>();
    }

    #endregion

    #region StartAsync - File Not Found Tests

    [Fact]
    public async Task StartAsync_WithNonExistentFile_ShouldThrowFileNotFoundException()
    {
        // Arrange
        var nonExistentPath = Path.Combine(_testDirectory, "nonexistent.csv");
        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = nonExistentPath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        var act = async () => await loader.StartAsync(CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<FileNotFoundException>()
            .WithMessage($"*{nonExistentPath}*");
    }

    #endregion

    #region StartAsync - Valid CSV Tests

    [Fact]
    public async Task StartAsync_WithValidCsv_ShouldLoadProducts()
    {
        // Arrange
        var csvContent = """
            productId,productName
            1,Product A
            2,Product B
            100,Product C
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        lookupService.Count.Should().Be(3);
        lookupService.GetProductName(1).Should().Be("Product A");
        lookupService.GetProductName(2).Should().Be("Product B");
        lookupService.GetProductName(100).Should().Be("Product C");
    }

    [Fact]
    public async Task StartAsync_WithValidCsv_ShouldLogLoadingStart()
    {
        // Arrange
        var csvContent = """
            productId,productName
            1,Product A
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.Should().Contain(log => log.Level == LogLevel.Information && log.Message.Contains("Loading product data"));
    }

    [Fact]
    public async Task StartAsync_WithValidCsv_ShouldLogLoadingComplete()
    {
        // Arrange
        var csvContent = """
            productId,productName
            1,Product A
            2,Product B
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.Should().Contain(log =>
            log.Level == LogLevel.Information &&
            log.Message.Contains("Successfully loaded") &&
            log.Message.Contains("2 products"));
    }

    #endregion

    #region StartAsync - Invalid Rows Tests

    [Fact]
    public async Task StartAsync_WithEmptyProductId_ShouldSkipRow()
    {
        // Arrange
        var csvContent = """
            productId,productName
            ,Product A
            2,Product B
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        lookupService.Count.Should().Be(1);
        lookupService.GetProductName(2).Should().Be("Product B");
    }

    [Fact]
    public async Task StartAsync_WithNonIntegerProductId_ShouldSkipRow()
    {
        // Arrange
        var csvContent = """
            productId,productName
            abc,Product A
            2,Product B
            3.5,Product C
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        lookupService.Count.Should().Be(1);
        lookupService.GetProductName(2).Should().Be("Product B");
    }

    [Fact]
    public async Task StartAsync_WithNegativeProductId_ShouldSkipRow()
    {
        // Arrange
        var csvContent = """
            productId,productName
            -1,Product A
            2,Product B
            -100,Product C
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        lookupService.Count.Should().Be(1);
        lookupService.GetProductName(2).Should().Be("Product B");
    }

    [Fact]
    public async Task StartAsync_WithZeroProductId_ShouldSkipRow()
    {
        // Arrange
        var csvContent = """
            productId,productName
            0,Product A
            2,Product B
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        lookupService.Count.Should().Be(1);
        lookupService.GetProductName(2).Should().Be("Product B");
    }

    [Fact]
    public async Task StartAsync_WithEmptyProductName_ShouldSkipRow()
    {
        // Arrange
        var csvContent = """
            productId,productName
            1,
            2,Product B
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        lookupService.Count.Should().Be(1);
        lookupService.GetProductName(2).Should().Be("Product B");
    }

    [Fact]
    public async Task StartAsync_WithInvalidRows_ShouldLogWarnings()
    {
        // Arrange
        var csvContent = """
            productId,productName
            ,Empty ID
            abc,Non-integer
            -1,Negative
            0,Zero
            1,
            2,Valid Product
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        var logs = _fakeLogger.Collector.GetSnapshot();
        var warningLogs = logs.Where(log => log.Level == LogLevel.Warning).ToList();
        warningLogs.Should().HaveCount(5);
        warningLogs.Should().Contain(log => log.Message.Contains("Skipping row"));
    }

    #endregion

    #region StartAsync - Duplicate ProductId Tests

    [Fact]
    public async Task StartAsync_WithDuplicateProductIds_ShouldKeepFirst()
    {
        // Arrange
        var csvContent = """
            productId,productName
            1,First Product
            1,Second Product
            1,Third Product
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        lookupService.Count.Should().Be(1);
        lookupService.GetProductName(1).Should().Be("First Product");
    }

    [Fact]
    public async Task StartAsync_WithDuplicateProductIds_ShouldLogWarnings()
    {
        // Arrange
        var csvContent = """
            productId,productName
            1,First Product
            1,Duplicate Product
            2,Valid Product
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        var logs = _fakeLogger.Collector.GetSnapshot();
        logs.Should().Contain(log =>
            log.Level == LogLevel.Warning &&
            log.Message.Contains("Duplicate productId") &&
            log.Message.Contains("1"));
    }

    #endregion

    #region StartAsync - Cancellation Tests

    [Fact]
    public async Task StartAsync_WithCancelledToken_ShouldThrowOperationCanceledException()
    {
        // Arrange
        var csvContent = """
            productId,productName
            1,Product A
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act
        var act = async () => await loader.StartAsync(cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    #endregion

    #region StartAsync - Edge Cases

    [Fact]
    public async Task StartAsync_WithEmptyCsv_ShouldLoadZeroProducts()
    {
        // Arrange
        var csvContent = """
            productId,productName
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        lookupService.Count.Should().Be(0);
    }

    [Fact]
    public async Task StartAsync_WithLargeProductIds_ShouldHandleCorrectly()
    {
        // Arrange
        var csvContent = $"""
            productId,productName
            {int.MaxValue},Max Product
            {int.MaxValue - 1},Almost Max
            1000000,Million Product
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        lookupService.Count.Should().Be(3);
        lookupService.GetProductName(int.MaxValue).Should().Be("Max Product");
        lookupService.GetProductName(int.MaxValue - 1).Should().Be("Almost Max");
        lookupService.GetProductName(1000000).Should().Be("Million Product");
    }

    [Fact]
    public async Task StartAsync_WithSpecialCharactersInProductNames_ShouldPreserveNames()
    {
        // Arrange
        var csvContent = """
            productId,productName
            1,Product with spaces
            2,Product-with-dashes
            3,Product_with_underscores
            4,Product (with) parentheses
            5,Product's apostrophe
            6,Produit français
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        lookupService.GetProductName(1).Should().Be("Product with spaces");
        lookupService.GetProductName(2).Should().Be("Product-with-dashes");
        lookupService.GetProductName(3).Should().Be("Product_with_underscores");
        lookupService.GetProductName(4).Should().Be("Product (with) parentheses");
        lookupService.GetProductName(5).Should().Be("Product's apostrophe");
        lookupService.GetProductName(6).Should().Be("Produit français");
    }

    [Fact]
    public async Task StartAsync_WithWhitespaceInData_ShouldTrimValues()
    {
        // Arrange
        var csvContent = """
            productId,productName
             1 , Product A
            2,Product B
            """;
        await File.WriteAllTextAsync(_testFilePath, csvContent);

        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        await loader.StartAsync(CancellationToken.None);

        // Assert
        lookupService.Count.Should().Be(2);
        lookupService.GetProductName(1).Should().Be("Product A");
        lookupService.GetProductName(2).Should().Be("Product B");
    }

    #endregion

    #region StopAsync Tests

    [Fact]
    public async Task StopAsync_ShouldCompleteSuccessfully()
    {
        // Arrange
        var lookupService = new CsvProductLookupService(new FakeLogger<CsvProductLookupService>());
        var options = Options.Create(new ProductDataOptions { FilePath = _testFilePath });
        var loader = new ProductDataLoader(lookupService, options, _fakeLogger);

        // Act
        var task = loader.StopAsync(CancellationToken.None);

        // Assert
        await task;
        task.IsCompletedSuccessfully.Should().BeTrue();
        task.Should().Be(Task.CompletedTask);
    }

    #endregion
}
