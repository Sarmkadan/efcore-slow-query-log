using EfCore.SlowQueryLog.Reporting;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Tests for the SlowQueryFingerprintRanking class.
/// </summary>
public class SlowQueryFingerprintRankingTests
{
    /// <summary>
    /// Creates a new SlowQuerySample instance with the specified duration.
    /// </summary>
    /// <param name="ms">The duration in milliseconds.</param>
    /// <param name="sql">The SQL query.</param>
    /// <returns>A new SlowQuerySample instance.</returns>
    private static SlowQuerySample Sample(int ms, string sql = "SELECT 1") => new()
    {
        Sql = sql,
        Duration = TimeSpan.FromMilliseconds(ms),
        CapturedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>
    /// Verifies that the SlowQueryFingerprintRanking orders fingerprints by average duration descending by default.
    /// </summary>
    [Fact]
    public void Orders_by_average_duration_descending_by_default()
    {
        var ranking = new SlowQueryFingerprintRanking(10);

        ranking.Add(Sample(500, "Query2"));
        ranking.Add(Sample(300, "Query3"));
        ranking.Add(Sample(100, "Query1"));

        var snap = ranking.Snapshot();

        Assert.Equal(3, snap.Count);
        Assert.Equal("Query2", snap[0].Sql);
        Assert.Equal("Query3", snap[1].Sql);
        Assert.Equal("Query1", snap[2].Sql);
    }

    /// <summary>
    /// Verifies that the SlowQueryFingerprintRanking respects the capacity and keeps the highest ranked fingerprints.
    /// </summary>
    [Fact]
    public void Respects_capacity_keeping_highest_ranked()
    {
        var ranking = new SlowQueryFingerprintRanking(2);

        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(500, "Query2"));
        ranking.Add(Sample(300, "Query3"));
        ranking.Add(Sample(700, "Query4"));

        var snap = ranking.Snapshot();

        Assert.Equal(2, snap.Count);
        Assert.Equal("Query4", snap[0].Sql);
        Assert.Equal("Query2", snap[1].Sql);
    }

    /// <summary>
    /// Verifies that the SlowQueryFingerprintRanking orders by total duration when configured.
    /// </summary>
    [Fact]
    public void Orders_by_total_duration_when_configured()
    {
        var ranking = new SlowQueryFingerprintRanking(10, SlowQueryFingerprintRanking.RankingMetric.TotalDuration);

        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(300, "Query2"));
        ranking.Add(Sample(50, "Query3"));
        ranking.Add(Sample(50, "Query3"));
        ranking.Add(Sample(50, "Query3"));

        var snap = ranking.Snapshot();

        Assert.Equal(3, snap.Count);
        Assert.Equal("Query2", snap[0].Sql);
        Assert.Equal("Query1", snap[1].Sql);
        Assert.Equal("Query3", snap[2].Sql);
    }

    /// <summary>
    /// Verifies that the SlowQueryFingerprintRanking orders by max duration when configured.
    /// </summary>
    [Fact]
    public void Orders_by_max_duration_when_configured()
    {
        var ranking = new SlowQueryFingerprintRanking(10, SlowQueryFingerprintRanking.RankingMetric.MaxDuration);

        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(500, "Query2"));
        ranking.Add(Sample(300, "Query2"));
        ranking.Add(Sample(700, "Query3"));

        var snap = ranking.Snapshot();

        Assert.Equal(3, snap.Count);
        Assert.Equal("Query3", snap[0].Sql);
        Assert.Equal("Query2", snap[1].Sql);
        Assert.Equal("Query1", snap[2].Sql);
    }

    /// <summary>
    /// Verifies that samples with the same SQL are grouped into a single fingerprint.
    /// </summary>
    [Fact]
    public void Groups_samples_with_same_sql()
    {
        var ranking = new SlowQueryFingerprintRanking(10);

        ranking.Add(Sample(100, "SELECT * FROM Users"));
        ranking.Add(Sample(200, "SELECT * FROM Users"));
        ranking.Add(Sample(150, "SELECT * FROM Users"));

        var snap = ranking.Snapshot();

        Assert.Single(snap);
        Assert.Equal("SELECT * FROM Users", snap[0].Sql);
        Assert.Equal(3, snap[0].SampleCount);
    }

    /// <summary>
    /// Verifies that samples with different SQL create separate fingerprints.
    /// </summary>
    [Fact]
    public void Creates_separate_fingerprints_for_different_sql()
    {
        var ranking = new SlowQueryFingerprintRanking(10);

        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(200, "Query2"));
        ranking.Add(Sample(150, "Query3"));

        var snap = ranking.Snapshot();

        Assert.Equal(3, snap.Count);
    }

    /// <summary>
    /// Verifies that fingerprint statistics are computed correctly.
    /// </summary>
    [Fact]
    public void Computes_fingerprint_statistics_correctly()
    {
        var ranking = new SlowQueryFingerprintRanking(10);

        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(300, "Query1"));
        ranking.Add(Sample(200, "Query1"));

        var snap = ranking.Snapshot();
        Assert.Single(snap);

        var fingerprint = snap[0];
        Assert.Equal(3, fingerprint.SampleCount);
        Assert.Equal(200, fingerprint.AverageDuration.TotalMilliseconds, 1);
        Assert.Equal(300, fingerprint.MaxDuration.TotalMilliseconds);
        Assert.Equal(100, fingerprint.MinDuration.TotalMilliseconds);
        Assert.Equal(600, fingerprint.TotalDuration.TotalMilliseconds);
    }

    /// <summary>
    /// Verifies that creating a SlowQueryFingerprintRanking with a capacity of 0 throws an ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void Zero_capacity_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlowQueryFingerprintRanking(0));
    }

    /// <summary>
    /// Verifies that Clear removes all fingerprints.
    /// </summary>
    [Fact]
    public void Clear_removes_all_fingerprints()
    {
        var ranking = new SlowQueryFingerprintRanking(10);

        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(200, "Query2"));

        Assert.Equal(2, ranking.Count);

        ranking.Clear();

        Assert.Equal(0, ranking.Count);
        Assert.Empty(ranking.Snapshot());
    }

    /// <summary>
    /// Verifies that the Metric property returns the configured ranking metric.
    /// </summary>
    [Fact]
    public void Metric_property_returns_configured_metric()
    {
        var avgRanking = new SlowQueryFingerprintRanking(10, SlowQueryFingerprintRanking.RankingMetric.AverageDuration);
        Assert.Equal(SlowQueryFingerprintRanking.RankingMetric.AverageDuration, avgRanking.Metric);

        var totalRanking = new SlowQueryFingerprintRanking(10, SlowQueryFingerprintRanking.RankingMetric.TotalDuration);
        Assert.Equal(SlowQueryFingerprintRanking.RankingMetric.TotalDuration, totalRanking.Metric);

        var maxRanking = new SlowQueryFingerprintRanking(10, SlowQueryFingerprintRanking.RankingMetric.MaxDuration);
        Assert.Equal(SlowQueryFingerprintRanking.RankingMetric.MaxDuration, maxRanking.Metric);

        var defaultRanking = new SlowQueryFingerprintRanking(10);
        Assert.Equal(SlowQueryFingerprintRanking.RankingMetric.AverageDuration, defaultRanking.Metric);
    }

    /// <summary>
    /// Verifies tie behavior - when fingerprints have equal ranking metric values.
    /// </summary>
    [Fact]
    public void Handles_ties_correctly()
    {
        var ranking = new SlowQueryFingerprintRanking(10);

        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(100, "Query2"));
        ranking.Add(Sample(100, "Query3"));

        var snap = ranking.Snapshot();

        Assert.Equal(3, snap.Count);
        Assert.Contains(snap, f => f.Sql == "Query1");
        Assert.Contains(snap, f => f.Sql == "Query2");
        Assert.Contains(snap, f => f.Sql == "Query3");
    }
}
