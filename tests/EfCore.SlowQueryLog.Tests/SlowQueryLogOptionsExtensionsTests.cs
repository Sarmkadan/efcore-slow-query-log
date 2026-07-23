using System;
using Microsoft.Extensions.Logging;
using Xunit;
using EfCore.SlowQueryLog.Options;

namespace EfCore.SlowQueryLog.Tests;

public class SlowQueryLogOptionsExtensionsTests
{
    [Fact]
    public void WithThresholdMilliseconds_SetsThresholdAndReturnsSameInstance()
    {
        var options = new SlowQueryLogOptions();

        var result = options.WithThresholdMilliseconds(250);

        Assert.Same(options, result);
        Assert.Equal(TimeSpan.FromMilliseconds(250), options.Threshold);
    }

    [Fact]
    public void WithThresholdMilliseconds_NullOptions_Throws()
    {
        SlowQueryLogOptions? options = null;
        Assert.Throws<ArgumentNullException>(() => options!.WithThresholdMilliseconds(100));
    }

    [Fact]
    public void WithThresholdMilliseconds_NonPositive_Throws()
    {
        var options = new SlowQueryLogOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WithThresholdMilliseconds(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WithThresholdMilliseconds(-5));
    }

    [Fact]
    public void WithThresholdSeconds_SetsThresholdAndReturnsSameInstance()
    {
        var options = new SlowQueryLogOptions();

        var result = options.WithThresholdSeconds(2);

        Assert.Same(options, result);
        Assert.Equal(TimeSpan.FromSeconds(2), options.Threshold);
    }

    [Fact]
    public void WithThresholdMinutes_SetsThresholdAndReturnsSameInstance()
    {
        var options = new SlowQueryLogOptions();

        var result = options.WithThresholdMinutes(3);

        Assert.Same(options, result);
        Assert.Equal(TimeSpan.FromMinutes(3), options.Threshold);
    }

    [Fact]
    public void WithLogLevel_SetsLogLevelAndReturnsSameInstance()
    {
        var options = new SlowQueryLogOptions();

        var result = options.WithLogLevel(LogLevel.Critical);

        Assert.Same(options, result);
        Assert.Equal(LogLevel.Critical, options.LogLevel);
    }

    [Fact]
    public void WithParameterValues_EnablesParameterLogging()
    {
        var options = new SlowQueryLogOptions { IncludeParameterValues = false };

        var result = options.WithParameterValues();

        Assert.Same(options, result);
        Assert.True(options.IncludeParameterValues);
    }

    [Fact]
    public void WithoutParameterValues_DisablesParameterLogging()
    {
        var options = new SlowQueryLogOptions { IncludeParameterValues = true };

        var result = options.WithoutParameterValues();

        Assert.Same(options, result);
        Assert.False(options.IncludeParameterValues);
    }

    [Fact]
    public void WithIndexSuggestions_EnablesSuggestions()
    {
        var options = new SlowQueryLogOptions { SuggestIndexes = false };

        var result = options.WithIndexSuggestions();

        Assert.Same(options, result);
        Assert.True(options.SuggestIndexes);
    }

    [Fact]
    public void WithoutIndexSuggestions_DisablesSuggestions()
    {
        var options = new SlowQueryLogOptions { SuggestIndexes = true };

        var result = options.WithoutIndexSuggestions();

        Assert.Same(options, result);
        Assert.False(options.SuggestIndexes);
    }

    [Fact]
    public void WithRankingCapacity_SetsCapacityAndReturnsSameInstance()
    {
        var options = new SlowQueryLogOptions();

        var result = options.WithRankingCapacity(42);

        Assert.Same(options, result);
        Assert.Equal(42, options.RankingCapacity);
    }

    [Fact]
    public void WithRankingCapacity_NonPositive_Throws()
    {
        var options = new SlowQueryLogOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WithRankingCapacity(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WithRankingCapacity(-1));
    }

    [Fact]
    public void WithMaxSamples_SetsMaxSamplesAndReturnsSameInstance()
    {
        var options = new SlowQueryLogOptions();

        var result = options.WithMaxSamples(1234);

        Assert.Same(options, result);
        Assert.Equal(1234, options.MaxSamples);
    }

    [Fact]
    public void WithMaxSamples_NonPositive_Throws()
    {
        var options = new SlowQueryLogOptions();
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WithMaxSamples(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => options.WithMaxSamples(-10));
    }

    [Fact]
    public void NullOptions_AllMethods_ThrowArgumentNullException()
    {
        SlowQueryLogOptions? nullOptions = null;

        Assert.Throws<ArgumentNullException>(() => nullOptions!.WithThresholdSeconds(1));
        Assert.Throws<ArgumentNullException>(() => nullOptions!.WithThresholdMinutes(1));
        Assert.Throws<ArgumentNullException>(() => nullOptions!.WithLogLevel(LogLevel.Information));
        Assert.Throws<ArgumentNullException>(() => nullOptions!.WithParameterValues());
        Assert.Throws<ArgumentNullException>(() => nullOptions!.WithoutParameterValues());
        Assert.Throws<ArgumentNullException>(() => nullOptions!.WithIndexSuggestions());
        Assert.Throws<ArgumentNullException>(() => nullOptions!.WithoutIndexSuggestions());
        Assert.Throws<ArgumentNullException>(() => nullOptions!.WithRankingCapacity(10));
        Assert.Throws<ArgumentNullException>(() => nullOptions!.WithMaxSamples(10));
    }
}
