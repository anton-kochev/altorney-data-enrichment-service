using FluentAssertions;
using Domain.ValueObjects;

namespace Domain.Tests.ValueObjects;

public class ProductIdentifierTests
{
    #region Create - Valid Tests

    [Theory]
    [InlineData(1)]
    [InlineData(100)]
    [InlineData(1000)]
    [InlineData(999999)]
    [InlineData(int.MaxValue)]
    public void Create_WithPositiveInteger_ShouldSucceed(int value)
    {
        // Act
        var productId = ProductIdentifier.Create(value);

        // Assert
        productId.Should().NotBeNull();
        productId.Value.Should().Be(value);
    }

    [Fact]
    public void Create_WithMaxInt_ShouldSucceed()
    {
        // Arrange
        var maxValue = int.MaxValue;

        // Act
        var productId = ProductIdentifier.Create(maxValue);

        // Assert
        productId.Should().NotBeNull();
        productId.Value.Should().Be(maxValue);
    }

    #endregion

    #region Create - Invalid Tests

    [Fact]
    public void Create_WithZero_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ProductIdentifier.Create(0);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be a positive integer*")
            .WithParameterName("productId");
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-100)]
    [InlineData(-999999)]
    [InlineData(int.MinValue)]
    public void Create_WithNegativeInteger_ShouldThrowArgumentException(int value)
    {
        // Act
        var act = () => ProductIdentifier.Create(value);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*must be a positive integer*")
            .WithParameterName("productId");
    }

    #endregion

    #region Value Tests

    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(12345)]
    public void Value_ShouldReturnCorrectInteger(int expectedValue)
    {
        // Arrange
        var productId = ProductIdentifier.Create(expectedValue);

        // Act
        var actualValue = productId.Value;

        // Assert
        actualValue.Should().Be(expectedValue);
    }

    #endregion

    #region Equality Tests

    [Fact]
    public void Equality_TwoProductIdentifiersWithSameValue_ShouldBeEqual()
    {
        // Arrange
        var productId1 = ProductIdentifier.Create(100);
        var productId2 = ProductIdentifier.Create(100);

        // Act & Assert
        productId1.Should().Be(productId2);
        productId1.Equals(productId2).Should().BeTrue();
        (productId1 == productId2).Should().BeTrue();
        (productId1 != productId2).Should().BeFalse();
    }

    [Fact]
    public void Equality_TwoProductIdentifiersWithDifferentValues_ShouldNotBeEqual()
    {
        // Arrange
        var productId1 = ProductIdentifier.Create(100);
        var productId2 = ProductIdentifier.Create(200);

        // Act & Assert
        productId1.Should().NotBe(productId2);
        productId1.Equals(productId2).Should().BeFalse();
        (productId1 == productId2).Should().BeFalse();
        (productId1 != productId2).Should().BeTrue();
    }

    [Fact]
    public void GetHashCode_TwoProductIdentifiersWithSameValue_ShouldHaveSameHashCode()
    {
        // Arrange
        var productId1 = ProductIdentifier.Create(100);
        var productId2 = ProductIdentifier.Create(100);

        // Act & Assert
        productId1.GetHashCode().Should().Be(productId2.GetHashCode());
    }

    #endregion
}
