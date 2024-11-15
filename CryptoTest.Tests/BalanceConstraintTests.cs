using CryptoTest.Services.StrategyService;
using FluentAssertions;

namespace CryptoTest.Tests;

public class BalanceConstraintTests
{
    [Fact]
    public void AmountToCheck_LessThanRemainingAmount_ShouldNotAdjust()
    {
        // Arrange
        var availableAmount = 100m;
        var amountUsed = 30m;
        var amountToCheck = 50m;

        // Act
        var result = CryptoTransactionStrategy.AreConstraintsRespected(availableAmount, amountUsed, amountToCheck);

        // Assert
        result.isAdjusted.Should().BeFalse();
        result.adjustedAmount.Should().Be(amountToCheck);
    }

    [Fact]
    public void AmountToCheck_EqualToRemainingAmount_ShouldNotAdjust()
    {
        // Arrange
        var availableAmount = 100m;
        var amountUsed = 70m;
        var amountToCheck = 30m;

        // Act
        var result = CryptoTransactionStrategy.AreConstraintsRespected(availableAmount, amountUsed, amountToCheck);

        // Assert
        result.isAdjusted.Should().BeFalse();
        result.adjustedAmount.Should().Be(amountToCheck);
    }

    [Fact]
    public void AmountToCheck_GreaterThanRemainingAmount_ShouldAdjust()
    {
        // Arrange
        var availableAmount = 100m;
        var amountUsed = 80m;
        var amountToCheck = 30m;

        // Act
        var result = CryptoTransactionStrategy.AreConstraintsRespected(availableAmount, amountUsed, amountToCheck);

        // Assert
        result.isAdjusted.Should().BeTrue();
        result.adjustedAmount.Should().Be(availableAmount - amountUsed);
    }

    [Fact]
    public void AmountUsed_EqualToAvailableAmount_ShouldAdjustToZero()
    {
        // Arrange
        var availableAmount = 100m;
        var amountUsed = 100m;
        var amountToCheck = 50m;

        // Act
        var result = CryptoTransactionStrategy.AreConstraintsRespected(availableAmount, amountUsed, amountToCheck);

        // Assert
        result.isAdjusted.Should().BeTrue();
        result.adjustedAmount.Should().Be(availableAmount - amountUsed);
    }

    [Fact]
    public void AmountToCheck_WithZeroAvailableAmount_ShouldAdjustToZero()
    {
        // Arrange
        var availableAmount = 0m;
        var amountUsed = 0m;
        var amountToCheck = 10m;

        // Act
        var result = CryptoTransactionStrategy.AreConstraintsRespected(availableAmount, amountUsed, amountToCheck);

        // Assert
        result.isAdjusted.Should().BeTrue();
        result.adjustedAmount.Should().Be(availableAmount);
    }

    [Fact]
    public void ZeroAmounts_ShouldNotAdjust()
    {
        // Arrange
        var availableAmount = 0m;
        var amountUsed = 0m;
        var amountToCheck = 0m;

        // Act
        var result = CryptoTransactionStrategy.AreConstraintsRespected(availableAmount, amountUsed, amountToCheck);

        // Assert
        result.isAdjusted.Should().BeFalse();
        result.adjustedAmount.Should().Be(0m);
    }
}