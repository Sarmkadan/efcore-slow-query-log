using System;
using System.Collections.Generic;
using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Reporting;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Unit tests for <see cref="SlowQueryFingerprintRanking"/> and the related
/// <see cref="SlowQueryFingerprint"/> aggregation class.
/// </summary>
public class SlowQueryFingerprintRankingTests
{
    // Helper to create a sample with the given duration (ms) and optional SQL.
    private static SlowQuerySample Sample(int ms, string sql = "SELECT 1") => new()
    {
        Sql = sql,
        Duration = TimeSpan.FromMilliseconds(ms),
        CapturedAt = DateTimeOffset.UtcNow,
        Parameters = null,
        Suggestions = Array.Empty<IndexSuggestion>()
    };

    [Fact]
    public void Constructor_ZeroCapacity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlowQueryFingerprintRanking(0));
    }

    [Fact]
    public void Add_SingleSample_CreatesFingerprintWithCorrectStatistics()
    {
        var ranking = new SlowQueryFingerprintRanking(10);
        ranking.Add(Sample(250, "SELECT * FROM Users"));

        var snap = ranking.Snapshot();
        Assert.Single(snap);

        var fp = snap[0];
        Assert.Equal("SELECT * FROM Users", fp.Sql);
        Assert.Equal(1, fp.SampleCount);
        Assert.Equal(250, fp.AverageDuration.TotalMilliseconds);
        Assert.Equal(250, fp.MaxDuration.TotalMilliseconds);
        Assert.Equal(250, fp.MinDuration.TotalMilliseconds);
        Assert.Equal(250, fp.TotalDuration.TotalMilliseconds);
    }

    [Fact]
    public void Add_MultipleSamples_GroupedBySql_AggregatesStatistics()
    {
        var ranking = new SlowQueryFingerprintRanking(10);
        ranking.Add(Sample(100, "Q"));
        ranking.Add(Sample(300, "Q"));
        ranking.Add(Sample(200, "Q"));

        var fp = ranking.Snapshot()[0];
        Assert.Equal(3, fp.SampleCount);
        Assert.Equal(200, fp.AverageDuration.TotalMilliseconds, 1);
        Assert.Equal(300, fp.MaxDuration.TotalMilliseconds);
        Assert.Equal(100, fp.MinDuration.TotalMilliseconds);
        Assert.Equal(600, fp.TotalDuration.TotalMilliseconds);
    }

    [Fact]
    public void Add_DifferentSql_CreatesSeparateFingerprints()
    {
        var ranking = new SlowQueryFingerprintRanking(10);
        ranking.Add(Sample(100, "A"));
        ranking.Add(Sample(200, "B"));
        ranking.Add(Sample(150, "C"));

        var snap = ranking.Snapshot();
        Assert.Equal(3, snap.Count);
        Assert.Contains(snap, f => f.Sql == "A");
        Assert.Contains(snap, f => f.Sql == "B");
        Assert.Contains(snap, f => f.Sql == "C");
    }

    [Fact]
    public void AddRange_EmptyCollection_NoEffect()
    {
        var ranking = new SlowQueryFingerprintRanking(10);
        ranking.AddRange(Array.Empty<SlowQuerySample>());
        Assert.Empty(ranking.Snapshot());
    }

    [Fact]
    public void AddRange_NullCollection_ThrowsArgumentNullException()
    {
        var ranking = new SlowQueryFingerprintRanking(10);
        Assert.Throws<ArgumentNullException>(() => ranking.AddRange(null!));
    }

    [Fact]
    public void Capacity_IsRespected_OnlyTopRankedKept()
    {
        var ranking = new SlowQueryFingerprintRanking(2);

        ranking.Add(Sample(100, "Q1"));
        ranking.Add(Sample(500, "Q2"));
        ranking.Add(Sample(300, "Q3"));
        ranking.Add(Sample(700, "Q4")); // highest

        var snap = ranking.Snapshot();
        Assert.Equal(2, snap.Count);
        Assert.Equal("Q4", snap[0].Sql);
        Assert.Equal("Q2", snap[1].Sql);
    }

    [Fact]
    public void Clear_RemovesAllFingerprints()
    {
        var ranking = new SlowQueryFingerprintRanking(10);
        ranking.Add(Sample(100, "A"));
        ranking.Add(Sample(200, "B"));
        Assert.Equal(2, ranking.Count);

        ranking.Clear();
        Assert.Equal(0, ranking.Count);
        Assert.Empty(ranking.Snapshot());
    }

    [Fact]
    public void MetricProperty_ReturnsConfiguredMetric()
    {
        var avg = new SlowQueryFingerprintRanking(10, SlowQueryFingerprintRanking.RankingMetric.AverageDuration);
        var total = new SlowQueryFingerprintRanking(10, SlowQueryFingerprintRanking.RankingMetric.TotalDuration);
        var max = new SlowQueryFingerprintRanking(10, SlowQueryFingerprintRanking.RankingMetric.MaxDuration);
        var p95 = new SlowQueryFingerprintRanking(10, SlowQueryFingerprintRanking.RankingMetric.P95Duration);

        Assert.Equal(SlowQueryFingerprintRanking.RankingMetric.AverageDuration, avg.Metric);
        Assert.Equal(SlowQueryFingerprintRanking.RankingMetric.TotalDuration, total.Metric);
        Assert.Equal(SlowQueryFingerprintRanking.RankingMetric.MaxDuration, max.Metric);
        Assert.Equal(SlowQueryFingerprintRanking.RankingMetric.P95Duration, p95.Metric);
    }

    [Fact]
    public void Add_NullSample_ThrowsArgumentNullException()
    {
        var ranking = new SlowQueryFingerprintRanking(10);
        Assert.Throws<ArgumentNullException>(() => ranking.Add(null!));
    }
}
