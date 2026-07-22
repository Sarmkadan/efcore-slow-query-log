using System;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Unit tests for <see cref="SlowQueryInterceptorValidation"/> static class.
/// </summary>
public class SlowQueryInterceptorValidationTests
{
    private static SlowQueryInterceptor CreateValidInterceptor()
    {
        var options = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromSeconds(1),
            MaxSamples = 100,
            RankingCapacity = 50
        };
        return new SlowQueryInterceptor(options);
    }

    [Fact]
    public void Validate_WithValidInterceptor_ReturnsEmptyList()
    {
        // Arrange
        var interceptor = CreateValidInterceptor();

        // Act
        var result = interceptor.Validate();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Validate_WithNullInterceptor_ThrowsArgumentNullException()
    {
        // Arrange
        SlowQueryInterceptor? interceptor = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => interceptor!.Validate());
    }

    [Fact]
    public void IsValid_WithValidInterceptor_ReturnsTrue()
    {
        // Arrange
        var interceptor = CreateValidInterceptor();

        // Act
        var result = interceptor.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValid_WithNullInterceptor_ReturnsFalse()
    {
        // Arrange
        SlowQueryInterceptor? interceptor = null;

        // Act
        var result = interceptor.IsValid();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void EnsureValid_WithValidInterceptor_DoesNotThrow()
    {
        // Arrange
        var interceptor = CreateValidInterceptor();

        // Act & Assert
        interceptor.EnsureValid(); // Should not throw
    }

    [Fact]
    public void EnsureValid_WithNullInterceptor_ThrowsArgumentNullException()
    {
        // Arrange
        SlowQueryInterceptor? interceptor = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => interceptor!.EnsureValid());
    }

    [Fact]
    public void Validate_ReturnsReadOnlyList()
    {
        // Arrange
        var interceptor = CreateValidInterceptor();

        // Act
        var result = interceptor.Validate();

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<string>>(result);
    }

    [Fact]
    public void IsValid_WithValidInterceptorAfterConstruction_ReturnsTrue()
    {
        // Arrange
        var options = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromSeconds(1),
            MaxSamples = 100,
            RankingCapacity = 50
        };
        var interceptor = new SlowQueryInterceptor(options);

        // Act
        var result = interceptor.IsValid();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void Validate_ReturnsNonNullList()
    {
        // Arrange
        var interceptor = CreateValidInterceptor();

        // Act
        var result = interceptor.Validate();

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public void Validate_WithValidInterceptor_ReturnsEmptyCollection()
    {
        // Arrange
        var interceptor = CreateValidInterceptor();

        // Act
        var result = interceptor.Validate();

        // Assert
        Assert.Empty(result);
    }
}