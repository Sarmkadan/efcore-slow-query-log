using EfCore.SlowQueryLog.Reporting;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Tests for fingerprint-based ranking functionality.
/// </summary>
public class SlowQueryRankingFingerprintTests
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
    /// Verifies that GetFingerprints groups samples by SQL and computes aggregated statistics.
    /// </summary>
    [Fact]
    public void GetFingerprints_groups_by_sql_and_computes_statistics()
    {
        var ranking = new SlowQueryRanking(10);

        // Add samples with same SQL
        ranking.Add(Sample(100, "SELECT * FROM Users WHERE Id = @id"));
        ranking.Add(Sample(200, "SELECT * FROM Users WHERE Id = @id"));
        ranking.Add(Sample(150, "SELECT * FROM Users WHERE Id = @id"));

        // Add samples with different SQL
        ranking.Add(Sample(300, "SELECT * FROM Orders WHERE CustomerId = @id"));
        ranking.Add(Sample(50, "SELECT * FROM Orders WHERE CustomerId = @id"));

        var fingerprints = ranking.GetFingerprints();

        Assert.Equal(2, fingerprints.Count);

        // First fingerprint should be for Orders (higher average)
        var ordersFingerprint = fingerprints[0];
        Assert.Equal("SELECT * FROM Orders WHERE CustomerId = @id", ordersFingerprint.Sql);
        Assert.Equal(2, ordersFingerprint.SampleCount);
        Assert.Equal(175, ordersFingerprint.AverageDuration.TotalMilliseconds, 1); // (300+50)/2
        Assert.Equal(300, ordersFingerprint.MaxDuration.TotalMilliseconds);
        Assert.Equal(50, ordersFingerprint.MinDuration.TotalMilliseconds);
        Assert.Equal(350, ordersFingerprint.TotalDuration.TotalMilliseconds);

        // Second fingerprint should be for Users
        var usersFingerprint = fingerprints[1];
        Assert.Equal("SELECT * FROM Users WHERE Id = @id", usersFingerprint.Sql);
        Assert.Equal(3, usersFingerprint.SampleCount);
        Assert.Equal(150, usersFingerprint.AverageDuration.TotalMilliseconds, 1); // (100+200+150)/3
        Assert.Equal(200, usersFingerprint.MaxDuration.TotalMilliseconds);
        Assert.Equal(100, usersFingerprint.MinDuration.TotalMilliseconds);
        Assert.Equal(450, usersFingerprint.TotalDuration.TotalMilliseconds);
    }

    /// <summary>
    /// Verifies that GetFingerprintsByTotalDuration orders by total duration.
    /// </summary>
    [Fact]
    public void GetFingerprintsByTotalDuration_orders_by_total_duration()
    {
        var ranking = new SlowQueryRanking(10);

        // Add samples where average is not the same as total
        ranking.Add(Sample(100, "Query1"));  // 1 sample, total = 100
        ranking.Add(Sample(100, "Query2"));  // 1 sample, total = 100
        ranking.Add(Sample(100, "Query2"));  // 2 samples, total = 200
        ranking.Add(Sample(50, "Query3"));   // 1 sample, total = 50

        var fingerprints = ranking.GetFingerprintsByTotalDuration();

        Assert.Equal(3, fingerprints.Count);
        Assert.Equal("Query2", fingerprints[0].Sql); // Total = 200
        Assert.Equal("Query1", fingerprints[1].Sql); // Total = 100
        Assert.Equal("Query3", fingerprints[2].Sql); // Total = 50
    }

    /// <summary>
    /// Verifies that GetFingerprintsByP95Duration orders by P95 duration.
    /// </summary>
    [Fact]
    public void GetFingerprintsByP95Duration_orders_by_p95_duration()
    {
        var ranking = new SlowQueryRanking(10);

        // Add samples with varying durations
        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(500, "Query1"));  // P95 should be close to 500
        ranking.Add(Sample(600, "Query1"));  // P95 should be 600

        var fingerprints = ranking.GetFingerprintsByP95Duration();

        Assert.Single(fingerprints);
        Assert.Equal("Query1", fingerprints[0].Sql);
        Assert.Equal(600, fingerprints[0].Percentile95.TotalMilliseconds);
    }

    /// <summary>
    /// Verifies that GetFingerprintsByMaxDuration orders by max duration.
    /// </summary>
    [Fact]
    public void GetFingerprintsByMaxDuration_orders_by_max_duration()
    {
        var ranking = new SlowQueryRanking(10);

        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(500, "Query2"));
        ranking.Add(Sample(300, "Query2"));

        var fingerprints = ranking.GetFingerprintsByMaxDuration();

        Assert.Equal(2, fingerprints.Count);
        Assert.Equal("Query2", fingerprints[0].Sql); // Max = 500
        Assert.Equal("Query1", fingerprints[1].Sql); // Max = 100
    }

    /// <summary>
    /// Verifies that P95 calculation works correctly for different sample sizes.
    /// </summary>
    [Fact]
    public void P95_calculation_works_correctly()
    {
        var ranking = new SlowQueryRanking(10);

        // Single sample - P95 should equal the sample
        ranking.Add(Sample(100, "Query1"));
        var fingerprints1 = ranking.GetFingerprints();
        Assert.Equal(100, fingerprints1[0].Percentile95.TotalMilliseconds);

        // Two samples - P95 should be the max
        ranking.Add(Sample(200, "Query1"));
        var fingerprints2 = ranking.GetFingerprints();
        Assert.Equal(200, fingerprints2[0].Percentile95.TotalMilliseconds);

        // Five samples - P95 should be close to max
        for (int i = 300; i <= 500; i += 50)
        {
            ranking.Add(Sample(i, "Query1"));
        }
        var fingerprints3 = ranking.GetFingerprints();
        Assert.True(fingerprints3[0].Percentile95.TotalMilliseconds >= 450); // Should be high
    }
}