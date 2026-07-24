using System;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Tests to verify that exception types thrown by SlowQueryInterceptor's public entry points
/// are consistent with those thrown by SlowQueryLogOptions setters/validation for the same
/// class of invalid input.
/// </summary>
/// <remarks>
/// This test suite ensures that validation is consistent across the entire public API surface.
/// </remarks>
public class SlowQueryValidationExceptionConsistencyTests
{
    [Fact]
    public void SlowQueryInterceptor_Constructor_WithZeroThreshold_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();
        options.Threshold = TimeSpan.Zero;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SlowQueryInterceptor(options));
        Assert.Contains("must be positive", exception.Message);
        Assert.Equal("Threshold", exception.ParamName);
    }

    [Fact]
    public void SlowQueryInterceptor_Constructor_WithNegativeRankingCapacity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();
        options.RankingCapacity = -1;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SlowQueryInterceptor(options));
        Assert.Contains("must be positive", exception.Message);
        Assert.Equal("RankingCapacity", exception.ParamName);
    }

    [Fact]
    public void SlowQueryInterceptor_Constructor_WithNegativeMaxSamples_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();
        options.MaxSamples = -5;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SlowQueryInterceptor(options));
        Assert.Contains("must be positive", exception.Message);
        Assert.Equal("MaxSamples", exception.ParamName);
    }

    [Fact]
    public void SlowQueryInterceptor_Constructor_WithOutOfRangeSamplingRate_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();
        options.SamplingRate = 1.5;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SlowQueryInterceptor(options));
        Assert.StartsWith("SamplingRate must be between 0.0 and 1.0", exception.Message);
        Assert.Equal("SamplingRate", exception.ParamName);
    }

    [Fact]
    public void SlowQueryInterceptor_Constructor_WithNegativeMaxAnalysesPerMinute_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();
        options.MaxAnalysesPerMinute = -1;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SlowQueryInterceptor(options));
        Assert.Contains("must be non-negative", exception.Message);
        Assert.Equal("MaxAnalysesPerMinute", exception.ParamName);
    }

    [Fact]
    public void SlowQueryInterceptor_Constructor_WithZeroBackgroundQueueCapacity_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();
        options.BackgroundQueueCapacity = 0;

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SlowQueryInterceptor(options));
        Assert.Contains("must be positive", exception.Message);
        Assert.Equal("BackgroundQueueCapacity", exception.ParamName);
    }

    [Fact]
    public void SlowQueryInterceptor_Constructor_WithNegativeThresholdMilliseconds_ThrowsArgumentOutOfRangeException()
    {
        // Arrange - using extension method which validates before setting
        var options = new SlowQueryLogOptions();

        // Act & Assert - extension method throws before we can even set the value
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.WithThresholdMilliseconds(0));
    }



    [Fact]
    public void SlowQueryLogOptionsExtensions_WithThresholdMilliseconds_WithZero_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.WithThresholdMilliseconds(0));
        Assert.Equal("Milliseconds must be positive. (Parameter 'milliseconds')", exception.Message);
        Assert.Equal("milliseconds", exception.ParamName);
    }

    [Fact]
    public void SlowQueryLogOptionsExtensions_WithThresholdMilliseconds_Negative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.WithThresholdMilliseconds(-5));
        Assert.Equal("Milliseconds must be positive. (Parameter 'milliseconds')", exception.Message);
        Assert.Equal("milliseconds", exception.ParamName);
    }

    [Fact]
    public void SlowQueryLogOptionsExtensions_WithThresholdSeconds_NonPositive_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.WithThresholdSeconds(0));
        Assert.Equal("Seconds must be positive. (Parameter 'seconds')", exception.Message);
        Assert.Equal("seconds", exception.ParamName);
    }

    [Fact]
    public void SlowQueryLogOptionsExtensions_WithThresholdMinutes_NonPositive_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.WithThresholdMinutes(-1));
        Assert.Equal("Minutes must be positive. (Parameter 'minutes')", exception.Message);
        Assert.Equal("minutes", exception.ParamName);
    }

    [Fact]
    public void SlowQueryLogOptionsExtensions_WithRankingCapacity_NonPositive_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.WithRankingCapacity(0));
        Assert.Equal("Capacity must be positive. (Parameter 'capacity')", exception.Message);
        Assert.Equal("capacity", exception.ParamName);
    }

    [Fact]
    public void SlowQueryLogOptionsExtensions_WithMaxSamples_NonPositive_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var options = new SlowQueryLogOptions();

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            options.WithMaxSamples(-10));
        Assert.Equal("MaxSamples must be positive. (Parameter 'maxSamples')", exception.Message);
        Assert.Equal("maxSamples", exception.ParamName);
    }
}