using FluentAssertions;
using Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;

namespace Infrastructure.Tests.Services;

public class CsvProductLookupServiceTests : IDisposable
{
    private readonly FakeLogger<CsvProductLookupService> _fakeLogger;
    private readonly CsvProductLookupService _sut;

    public CsvProductLookupServiceTests()
    {
        _fakeLogger = new FakeLogger<CsvProductLookupService>();
        _sut = new CsvProductLookupService(_fakeLogger);
    }

    public void Dispose()
    {
        // Cleanup if needed
    }

    #region Before LoadProducts Tests

    [Fact]
    public void Count_BeforeLoadProducts_ShouldBeZero()
    {
        // Arrange & Act
        var count = _sut.Count;

        // Assert
        count.Should().Be(0);
    }

    [Fact]
    public void IsLoaded_BeforeLoadProducts_ShouldBeFalse()
    {
        // Arrange & Act
        var isLoaded = _sut.IsLoaded;

        // Assert
        isLoaded.Should().BeFalse();
    }

    [Fact]
    public void GetProductName_BeforeLoadProducts_ShouldReturnNull()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = _sut.GetProductName(productId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryGetProductName_BeforeLoadProducts_ShouldReturnFalse()
    {
        // Arrange
        var productId = 1;

        // Act
        var result = _sut.TryGetProductName(productId, out var productName);

        // Assert
        result.Should().BeFalse();
        productName.Should().BeNull();
    }

    #endregion

    #region After LoadProducts with Valid Data Tests

    [Fact]
    public void LoadProducts_WithValidData_ShouldSetCountCorrectly()
    {
        // Arrange
        var products = CreateTestProducts(5);

        // Act
        InvokeLoadProducts(_sut, products);

        // Assert
        _sut.Count.Should().Be(5);
    }

    [Fact]
    public void LoadProducts_WithValidData_ShouldSetIsLoadedToTrue()
    {
        // Arrange
        var products = CreateTestProducts(3);

        // Act
        InvokeLoadProducts(_sut, products);

        // Assert
        _sut.IsLoaded.Should().BeTrue();
    }

    [Fact]
    public void LoadProducts_WithValidData_ShouldLogInformation()
    {
        // Arrange
        var products = CreateTestProducts(10);

        // Act
        InvokeLoadProducts(_sut, products);

        // Assert
        var logEntry = _fakeLogger.Collector.LatestRecord;
        logEntry.Should().NotBeNull();
        logEntry.Level.Should().Be(LogLevel.Information);
        logEntry.Message.Should().Contain("Loaded 10 products into lookup service");
    }

    [Fact]
    public void GetProductName_WithExistingProductId_ShouldReturnCorrectName()
    {
        // Arrange
        var products = new Dictionary<int, string>
        {
            { 1, "Product A" },
            { 2, "Product B" },
            { 100, "Product C" }
        };
        InvokeLoadProducts(_sut, products);

        // Act
        var result1 = _sut.GetProductName(1);
        var result2 = _sut.GetProductName(2);
        var result100 = _sut.GetProductName(100);

        // Assert
        result1.Should().Be("Product A");
        result2.Should().Be("Product B");
        result100.Should().Be("Product C");
    }

    [Fact]
    public void TryGetProductName_WithExistingProductId_ShouldReturnTrueAndCorrectName()
    {
        // Arrange
        var products = new Dictionary<int, string>
        {
            { 42, "The Answer" },
            { 99, "Bottles of Beer" }
        };
        InvokeLoadProducts(_sut, products);

        // Act
        var result42 = _sut.TryGetProductName(42, out var name42);
        var result99 = _sut.TryGetProductName(99, out var name99);

        // Assert
        result42.Should().BeTrue();
        name42.Should().Be("The Answer");

        result99.Should().BeTrue();
        name99.Should().Be("Bottles of Beer");
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(100)]
    [InlineData(1000)]
    public void LoadProducts_WithDifferentSizes_ShouldSetCountCorrectly(int count)
    {
        // Arrange
        var products = CreateTestProducts(count);

        // Act
        InvokeLoadProducts(_sut, products);

        // Assert
        _sut.Count.Should().Be(count);
        _sut.IsLoaded.Should().BeTrue();
    }

    #endregion

    #region Non-Existent ProductId Tests

    [Fact]
    public void GetProductName_WithNonExistentProductId_ShouldReturnNull()
    {
        // Arrange
        var products = new Dictionary<int, string>
        {
            { 1, "Product A" },
            { 2, "Product B" }
        };
        InvokeLoadProducts(_sut, products);
        var nonExistentId = 999;

        // Act
        var result = _sut.GetProductName(nonExistentId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public void TryGetProductName_WithNonExistentProductId_ShouldReturnFalse()
    {
        // Arrange
        var products = new Dictionary<int, string>
        {
            { 1, "Product A" },
            { 2, "Product B" }
        };
        InvokeLoadProducts(_sut, products);
        var nonExistentId = 999;

        // Act
        var result = _sut.TryGetProductName(nonExistentId, out var productName);

        // Assert
        result.Should().BeFalse();
        productName.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void GetProductName_WithNegativeOrZeroProductId_ShouldReturnNull(int productId)
    {
        // Arrange
        var products = new Dictionary<int, string>
        {
            { 1, "Product A" },
            { 2, "Product B" }
        };
        InvokeLoadProducts(_sut, products);

        // Act
        var result = _sut.GetProductName(productId);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void TryGetProductName_WithNegativeOrZeroProductId_ShouldReturnFalse(int productId)
    {
        // Arrange
        var products = new Dictionary<int, string>
        {
            { 1, "Product A" },
            { 2, "Product B" }
        };
        InvokeLoadProducts(_sut, products);

        // Act
        var result = _sut.TryGetProductName(productId, out var productName);

        // Assert
        result.Should().BeFalse();
        productName.Should().BeNull();
    }

    #endregion

    #region Empty Dictionary Tests

    [Fact]
    public void LoadProducts_WithEmptyDictionary_ShouldSetCountToZero()
    {
        // Arrange
        var emptyProducts = new Dictionary<int, string>();

        // Act
        InvokeLoadProducts(_sut, emptyProducts);

        // Assert
        _sut.Count.Should().Be(0);
    }

    [Fact]
    public void LoadProducts_WithEmptyDictionary_ShouldSetIsLoadedToFalse()
    {
        // Arrange
        var emptyProducts = new Dictionary<int, string>();

        // Act
        InvokeLoadProducts(_sut, emptyProducts);

        // Assert
        _sut.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public void LoadProducts_WithEmptyDictionary_ShouldLogZeroCount()
    {
        // Arrange
        var emptyProducts = new Dictionary<int, string>();

        // Act
        InvokeLoadProducts(_sut, emptyProducts);

        // Assert
        var logEntry = _fakeLogger.Collector.LatestRecord;
        logEntry.Should().NotBeNull();
        logEntry.Level.Should().Be(LogLevel.Information);
        logEntry.Message.Should().Contain("Loaded 0 products into lookup service");
    }

    #endregion

    #region Multiple LoadProducts Tests

    [Fact]
    public void LoadProducts_CalledMultipleTimes_ShouldReplaceData()
    {
        // Arrange
        var initialProducts = new Dictionary<int, string>
        {
            { 1, "Initial Product A" },
            { 2, "Initial Product B" }
        };
        var updatedProducts = new Dictionary<int, string>
        {
            { 10, "Updated Product X" },
            { 20, "Updated Product Y" },
            { 30, "Updated Product Z" }
        };

        // Act - Load initial data
        InvokeLoadProducts(_sut, initialProducts);
        var countAfterFirst = _sut.Count;
        var product1AfterFirst = _sut.GetProductName(1);

        // Act - Load updated data
        InvokeLoadProducts(_sut, updatedProducts);
        var countAfterSecond = _sut.Count;
        var product1AfterSecond = _sut.GetProductName(1);
        var product10AfterSecond = _sut.GetProductName(10);

        // Assert
        countAfterFirst.Should().Be(2);
        product1AfterFirst.Should().Be("Initial Product A");

        countAfterSecond.Should().Be(3);
        product1AfterSecond.Should().BeNull(); // Old product should be gone
        product10AfterSecond.Should().Be("Updated Product X"); // New product should exist
    }

    [Fact]
    public void LoadProducts_CalledMultipleTimes_ShouldUpdateCountAndIsLoaded()
    {
        // Arrange
        var firstProducts = CreateTestProducts(5);
        var secondProducts = CreateTestProducts(10);
        var emptyProducts = new Dictionary<int, string>();

        // Act & Assert - First load
        InvokeLoadProducts(_sut, firstProducts);
        _sut.Count.Should().Be(5);
        _sut.IsLoaded.Should().BeTrue();

        // Act & Assert - Second load
        InvokeLoadProducts(_sut, secondProducts);
        _sut.Count.Should().Be(10);
        _sut.IsLoaded.Should().BeTrue();

        // Act & Assert - Empty load
        InvokeLoadProducts(_sut, emptyProducts);
        _sut.Count.Should().Be(0);
        _sut.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public void LoadProducts_CalledMultipleTimes_ShouldLogEachLoad()
    {
        // Arrange
        var firstProducts = CreateTestProducts(3);
        var secondProducts = CreateTestProducts(7);

        // Act
        InvokeLoadProducts(_sut, firstProducts);
        InvokeLoadProducts(_sut, secondProducts);

        // Assert
        var allLogs = _fakeLogger.Collector.GetSnapshot();
        allLogs.Should().HaveCount(2);
        allLogs[0].Message.Should().Contain("Loaded 3 products into lookup service");
        allLogs[1].Message.Should().Contain("Loaded 7 products into lookup service");
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void LoadProducts_WithLargeProductIds_ShouldHandleCorrectly()
    {
        // Arrange
        var products = new Dictionary<int, string>
        {
            { int.MaxValue, "Max Product" },
            { int.MaxValue - 1, "Almost Max Product" },
            { 1000000, "Million Product" }
        };

        // Act
        InvokeLoadProducts(_sut, products);

        // Assert
        _sut.GetProductName(int.MaxValue).Should().Be("Max Product");
        _sut.GetProductName(int.MaxValue - 1).Should().Be("Almost Max Product");
        _sut.GetProductName(1000000).Should().Be("Million Product");
    }

    [Fact]
    public void LoadProducts_WithSpecialCharactersInProductNames_ShouldPreserveNames()
    {
        // Arrange
        var products = new Dictionary<int, string>
        {
            { 1, "Product with spaces" },
            { 2, "Product-with-dashes" },
            { 3, "Product_with_underscores" },
            { 4, "Product (with) parentheses" },
            { 5, "Product's apostrophe" },
            { 6, "Produit français" },
            { 7, "" } // Empty string
        };

        // Act
        InvokeLoadProducts(_sut, products);

        // Assert
        _sut.GetProductName(1).Should().Be("Product with spaces");
        _sut.GetProductName(2).Should().Be("Product-with-dashes");
        _sut.GetProductName(3).Should().Be("Product_with_underscores");
        _sut.GetProductName(4).Should().Be("Product (with) parentheses");
        _sut.GetProductName(5).Should().Be("Product's apostrophe");
        _sut.GetProductName(6).Should().Be("Produit français");
        _sut.GetProductName(7).Should().Be("");
    }

    #endregion

    #region Null Argument Tests

    [Fact]
    public void LoadProducts_WithNullDictionary_ShouldThrowArgumentNullException()
    {
        // Act
        var act = () => InvokeLoadProducts(_sut, null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("products");
    }

    #endregion

    #region Helper Methods

    private static Dictionary<int, string> CreateTestProducts(int count)
    {
        var products = new Dictionary<int, string>();
        for (int i = 1; i <= count; i++)
        {
            products[i] = $"Product {i}";
        }
        return products;
    }

    /// <summary>
    /// Calls the internal LoadProducts method (accessible via InternalsVisibleTo)
    /// </summary>
    private static void InvokeLoadProducts(CsvProductLookupService service, Dictionary<int, string> products)
    {
        service.LoadProducts(products);
    }

    #endregion
}
