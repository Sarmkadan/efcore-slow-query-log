using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Xunit;
using EfCore.SlowQueryLog.Options;
using EfCore.SlowQueryLog;

namespace EfCore.SlowQueryLog.Tests;

public class SlowQueryLogOptionsTests
{
    [Fact]
    public void DefaultOptions_PassValidation()
    {
        var options = new SlowQueryLogOptions();
        // Should not throw
        var exception = Record.Exception(() => options.Validate());
        Assert.Null(exception);
    }

    [Fact]
    public void Threshold_Zero_Throws()
    {
        var options = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.Zero
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void RankingCapacity_NonPositive_Throws()
    {
        var options = new SlowQueryLogOptions
        {
            RankingCapacity = 0
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void MaxSamples_NonPositive_Throws()
    {
        var options = new SlowQueryLogOptions
        {
            MaxSamples = -5
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void MaxAnalysesPerMinute_Negative_Throws()
    {
        var options = new SlowQueryLogOptions
        {
            MaxAnalysesPerMinute = -1
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void BackgroundQueueCapacity_NonPositive_Throws()
    {
        var options = new SlowQueryLogOptions
        {
            BackgroundQueueCapacity = 0
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Theory]
    [InlineData(-0.1)]
    [InlineData(1.1)]
    public void SamplingRate_OutOfRange_Throws(double rate)
    {
        var options = new SlowQueryLogOptions
        {
            SamplingRate = rate
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void ProviderThresholds_Null_DoesNotThrow()
    {
        var options = new SlowQueryLogOptions
        {
            ProviderThresholds = null!
        };

        var exception = Record.Exception(() => options.Validate());
        Assert.Null(exception);
    }

    [Fact]
    public void ProviderThresholds_EmptyKey_Throws()
    {
        var options = new SlowQueryLogOptions
        {
            ProviderThresholds = new Dictionary<string, TimeSpan>
            {
                { "", TimeSpan.FromSeconds(1) }
            }
        };

        Assert.Throws<ArgumentException>(() => options.Validate());
    }

    [Fact]
    public void ProviderThresholds_NonPositiveValue_Throws()
    {
        var options = new SlowQueryLogOptions
        {
            ProviderThresholds = new Dictionary<string, TimeSpan>
            {
                { "SqliteConnection", TimeSpan.Zero }
            }
        };

        Assert.Throws<ArgumentOutOfRangeException>(() => options.Validate());
    }

    [Fact]
    public void ValidCustomConfiguration_PassesValidation()
    {
        var options = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(250),
            LogLevel = LogLevel.Information,
            IncludeParameterValues = true,
            SuggestIndexes = false,
            RankingCapacity = 10,
            OnSlowQuery = sample => { /* no‑op */ },
            RedactParameters = false,
            SamplingRate = 0.5,
            MaxSamples = 500,
            MaxAnalysesPerMinute = 200,
            AnalyzeOnBackgroundThread = false,
            BackgroundQueueCapacity = 200,
            ProviderThresholds = new Dictionary<string, TimeSpan>
            {
                { "SqliteConnection", TimeSpan.FromMilliseconds(300) }
            }
        };

        var exception = Record.Exception(() => options.Validate());
        Assert.Null(exception);
    }
}
