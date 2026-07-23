using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit;
using EfCore.SlowQueryLog.Options;

namespace EfCore.SlowQueryLog.Tests;

public class SlowQueryLogOptionsJsonExtensionsTests
{
    [Fact]
    public void ToJson_WithDefaultOptions_ReturnsValidJson()
    {
        // Arrange
        var options = new SlowQueryLogOptions();

        // Act
        var json = options.ToJson();

        // Assert
        Assert.NotNull(json);
        Assert.NotEmpty(json);
        Assert.Contains("{", json);
        Assert.Contains("}", json);
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsFormattedJson()
    {
        // Arrange
        var options = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromSeconds(1),
            LogLevel = LogLevel.Warning,
            IncludeParameterValues = true
        };

        // Act
        var json = options.ToJson(indented: true);

        // Assert
        Assert.NotNull(json);
        Assert.Contains("\n", json);
        Assert.Contains("  ", json);
    }

    [Fact]
    public void ToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        // Arrange
        var options = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromSeconds(2),
            LogLevel = LogLevel.Error
        };

        // Act
        var json = options.ToJson(indented: false);

        // Assert
        Assert.NotNull(json);
        Assert.DoesNotContain("\n", json);
        Assert.DoesNotContain("  ", json);
    }

    [Fact]
    public void ToJson_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        SlowQueryLogOptions? options = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => options!.ToJson());
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsDeserializedOptions()
    {
        // Arrange
        var originalOptions = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(250),
            LogLevel = LogLevel.Information,
            IncludeParameterValues = true,
            SuggestIndexes = false,
            RankingCapacity = 10,
            MaxSamples = 500,
            MaxAnalysesPerMinute = 200,
            SamplingRate = 0.5,
            BackgroundQueueCapacity = 200,
            RedactParameters = false,
            ProviderThresholds = new Dictionary<string, TimeSpan>
            {
                { "SqliteConnection", TimeSpan.FromMilliseconds(300) }
            }
        };

        var json = originalOptions.ToJson();

        // Act
        var deserializedOptions = SlowQueryLogOptionsJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserializedOptions);
        Assert.Equal(originalOptions.Threshold, deserializedOptions.Threshold);
        Assert.Equal(originalOptions.LogLevel, deserializedOptions.LogLevel);
        Assert.Equal(originalOptions.IncludeParameterValues, deserializedOptions.IncludeParameterValues);
        Assert.Equal(originalOptions.SuggestIndexes, deserializedOptions.SuggestIndexes);
        Assert.Equal(originalOptions.RankingCapacity, deserializedOptions.RankingCapacity);
        Assert.Equal(originalOptions.MaxSamples, deserializedOptions.MaxSamples);
        Assert.Equal(originalOptions.MaxAnalysesPerMinute, deserializedOptions.MaxAnalysesPerMinute);
        Assert.Equal(originalOptions.SamplingRate, deserializedOptions.SamplingRate);
        Assert.Equal(originalOptions.BackgroundQueueCapacity, deserializedOptions.BackgroundQueueCapacity);
        Assert.Equal(originalOptions.RedactParameters, deserializedOptions.RedactParameters);
        Assert.Equal(originalOptions.ProviderThresholds, deserializedOptions.ProviderThresholds);
    }

    [Fact]
    public void FromJson_NullJson_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SlowQueryLogOptionsJsonExtensions.FromJson(null!));
    }

    [Fact]
    public void FromJson_EmptyJson_ReturnsNull()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var result = SlowQueryLogOptionsJsonExtensions.FromJson(emptyJson);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_WhitespaceJson_ThrowsJsonException()
    {
        // Arrange
        var whitespaceJson = "   \n\t  ";

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => SlowQueryLogOptionsJsonExtensions.FromJson(whitespaceJson));
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var invalidJson = "{ invalid json";

        // Act & Assert
        Assert.Throws<System.Text.Json.JsonException>(() => SlowQueryLogOptionsJsonExtensions.FromJson(invalidJson));
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndDeserializedOptions()
    {
        // Arrange
        var originalOptions = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromSeconds(1),
            LogLevel = LogLevel.Debug,
            IncludeParameterValues = false
        };

        var json = originalOptions.ToJson();

        // Act
        var result = SlowQueryLogOptionsJsonExtensions.TryFromJson(json, out var deserializedOptions);

        // Assert
        Assert.True(result);
        Assert.NotNull(deserializedOptions);
        Assert.Equal(originalOptions.Threshold, deserializedOptions!.Threshold);
        Assert.Equal(originalOptions.LogLevel, deserializedOptions.LogLevel);
        Assert.Equal(originalOptions.IncludeParameterValues, deserializedOptions.IncludeParameterValues);
    }

    [Fact]
    public void TryFromJson_NullJson_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => SlowQueryLogOptionsJsonExtensions.TryFromJson(null!, out _));
    }

    [Fact]
    public void TryFromJson_EmptyJson_ReturnsFalseAndNull()
    {
        // Arrange
        var emptyJson = "";

        // Act
        var result = SlowQueryLogOptionsJsonExtensions.TryFromJson(emptyJson, out var deserializedOptions);

        // Assert
        Assert.False(result);
        Assert.Null(deserializedOptions);
    }

    [Fact]
    public void TryFromJson_WhitespaceJson_ReturnsFalseAndNull()
    {
        // Arrange
        var whitespaceJson = "   \n\t  ";

        // Act
        var result = SlowQueryLogOptionsJsonExtensions.TryFromJson(whitespaceJson, out var deserializedOptions);

        // Assert
        Assert.False(result);
        Assert.Null(deserializedOptions);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        // Arrange
        var invalidJson = "{ invalid json";

        // Act
        var result = SlowQueryLogOptionsJsonExtensions.TryFromJson(invalidJson, out var deserializedOptions);

        // Assert
        Assert.False(result);
        Assert.Null(deserializedOptions);
    }

    [Fact]
    public void RoundTripSerialization_PreservesAllProperties()
    {
        // Arrange
        var originalOptions = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(123),
            LogLevel = LogLevel.Trace,
            IncludeParameterValues = true,
            SuggestIndexes = true,
            RankingCapacity = 99,
            MaxSamples = 1000,
            MaxAnalysesPerMinute = 50,
            SamplingRate = 0.75,
            BackgroundQueueCapacity = 150,
            RedactParameters = true,
            AnalyzeOnBackgroundThread = true,
            ProviderThresholds = new Dictionary<string, TimeSpan>
            {
                { "SqlServer", TimeSpan.FromSeconds(1) },
                { "Postgres", TimeSpan.FromSeconds(2) },
                { "MySql", TimeSpan.FromSeconds(3) }
            }
        };

        // Act
        var json = originalOptions.ToJson();
        var deserializedOptions = SlowQueryLogOptionsJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserializedOptions);
        Assert.Equal(originalOptions.Threshold, deserializedOptions.Threshold);
        Assert.Equal(originalOptions.LogLevel, deserializedOptions.LogLevel);
        Assert.Equal(originalOptions.IncludeParameterValues, deserializedOptions.IncludeParameterValues);
        Assert.Equal(originalOptions.SuggestIndexes, deserializedOptions.SuggestIndexes);
        Assert.Equal(originalOptions.RankingCapacity, deserializedOptions.RankingCapacity);
        Assert.Equal(originalOptions.MaxSamples, deserializedOptions.MaxSamples);
        Assert.Equal(originalOptions.MaxAnalysesPerMinute, deserializedOptions.MaxAnalysesPerMinute);
        Assert.Equal(originalOptions.SamplingRate, deserializedOptions.SamplingRate);
        Assert.Equal(originalOptions.BackgroundQueueCapacity, deserializedOptions.BackgroundQueueCapacity);
        Assert.Equal(originalOptions.RedactParameters, deserializedOptions.RedactParameters);
        Assert.Equal(originalOptions.AnalyzeOnBackgroundThread, deserializedOptions.AnalyzeOnBackgroundThread);
        Assert.Equal(originalOptions.ProviderThresholds, deserializedOptions.ProviderThresholds);
    }

    [Fact]
    public void RoundTripSerialization_WithTryFromJson_PreservesAllProperties()
    {
        // Arrange
        var originalOptions = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromSeconds(5),
            LogLevel = LogLevel.Critical,
            IncludeParameterValues = false,
            SuggestIndexes = false
        };

        var json = originalOptions.ToJson();

        // Act
        var success = SlowQueryLogOptionsJsonExtensions.TryFromJson(json, out var deserializedOptions);

        // Assert
        Assert.True(success);
        Assert.NotNull(deserializedOptions);
        Assert.Equal(originalOptions.Threshold, deserializedOptions!.Threshold);
        Assert.Equal(originalOptions.LogLevel, deserializedOptions.LogLevel);
        Assert.Equal(originalOptions.IncludeParameterValues, deserializedOptions.IncludeParameterValues);
        Assert.Equal(originalOptions.SuggestIndexes, deserializedOptions.SuggestIndexes);
    }

    [Fact]
    public void JsonUsesCamelCaseNamingPolicy()
    {
        // Arrange
        var options = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromSeconds(1),
            LogLevel = LogLevel.Information
        };

        // Act
        var json = options.ToJson();

        // Assert - check that camelCase properties are used (threshold, logLevel)
        Assert.Contains("threshold", json);
        Assert.Contains("logLevel", json);
    }
}
