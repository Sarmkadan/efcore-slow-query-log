using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Reporting;
using Xunit;

using System.Linq;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Tests for the SlowQueryRanking class.
/// </summary>
public class SlowQueryRankingTests
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
    /// Verifies that the SlowQueryRanking orders the samples by duration in descending order.
    /// </summary>
    [Fact]
    public void Orders_by_duration_descending()
    {
        var ranking = new SlowQueryRanking(10);
        ranking.Add(Sample(100));
        ranking.Add(Sample(900));
        ranking.Add(Sample(500));

        var snap = ranking.Snapshot();

        Assert.Equal(900, snap[0].Duration.TotalMilliseconds);
        Assert.Equal(500, snap[1].Duration.TotalMilliseconds);
        Assert.Equal(100, snap[2].Duration.TotalMilliseconds);
    }

    /// <summary>
    /// Verifies that the SlowQueryRanking respects the capacity and keeps the slowest samples.
    /// </summary>
    [Fact]
    public void Respects_capacity_keeping_slowest()
    {
        var ranking = new SlowQueryRanking(2);
        ranking.Add(Sample(100));
        ranking.Add(Sample(900));
        ranking.Add(Sample(500));

        var snap = ranking.Snapshot();

        Assert.Equal(2, snap.Count);
        Assert.Equal(900, snap[0].Duration.TotalMilliseconds);
        Assert.Equal(500, snap[1].Duration.TotalMilliseconds);
    }

    /// <summary>
    /// Verifies that the SlowQueryRanking computes percentiles correctly.
    /// </summary>
    [Fact]
    public void Computes_percentiles_correctly()
    {
        var ranking = new SlowQueryRanking(10);
        var sql = "SELECT 1";
        
        // Add 10 samples: 10, 20, 30, ..., 100
        for (int i = 1; i <= 10; i++)
        {
            ranking.Add(new SlowQuerySample { Sql = sql, Duration = TimeSpan.FromMilliseconds(i * 10), CapturedAt = DateTimeOffset.UtcNow });
        }

        var fingerprints = ranking.GetFingerprints();
        Assert.Single(fingerprints);
        var f = fingerprints[0];
        
        Assert.Equal(50, f.Percentile50.TotalMilliseconds);
        Assert.Equal(100, f.Percentile95.TotalMilliseconds);
        Assert.Equal(100, f.Percentile99.TotalMilliseconds);
    }

    /// <summary>
    /// Verifies that creating a SlowQueryRanking with a capacity of 0 throws an ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void Zero_capacity_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlowQueryRanking(0));
    }

    /// <summary>
    /// Verifies that GetFingerprints orders fingerprints by average duration descending.
    /// </summary>
    [Fact]
    public void GetFingerprints_orders_by_average_duration_descending()
    {
        var ranking = new SlowQueryRanking(10);

        // Add samples with different SQL queries
        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(200, "Query2"));
        ranking.Add(Sample(150, "Query3"));

        var fingerprints = ranking.GetFingerprints();
        Assert.Equal(3, fingerprints.Count);
        Assert.Equal("Query2", fingerprints[0].Sql); // Avg: 200
        Assert.Equal("Query3", fingerprints[1].Sql); // Avg: 150
        Assert.Equal("Query1", fingerprints[2].Sql); // Avg: 100
    }

    /// <summary>
    /// Verifies that GetFingerprints respects capacity for fingerprints.
    /// </summary>
    [Fact]
    public void GetFingerprints_respects_capacity()
    {
        var ranking = new SlowQueryRanking(2);

        // Add more than capacity samples
        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(200, "Query2"));
        ranking.Add(Sample(150, "Query3"));
        ranking.Add(Sample(300, "Query4"));

        var fingerprints = ranking.GetFingerprints();
        Assert.Equal(2, fingerprints.Count);
        Assert.Equal("Query4", fingerprints[0].Sql); // Avg: 300
        Assert.Equal("Query2", fingerprints[1].Sql); // Avg: 200
    }

    /// <summary>
    /// Verifies that GetFingerprints groups samples by SQL correctly.
    /// </summary>
    [Fact]
    public void GetFingerprints_groups_by_sql()
    {
        var ranking = new SlowQueryRanking(10);

        ranking.Add(Sample(100, "SELECT * FROM Users"));
        ranking.Add(Sample(200, "SELECT * FROM Orders"));
        ranking.Add(Sample(150, "SELECT * FROM Users")); // Same SQL as first
        ranking.Add(Sample(300, "SELECT * FROM Orders")); // Same SQL as second

        var fingerprints = ranking.GetFingerprints();
        Assert.Equal(2, fingerprints.Count);

        var usersFingerprint = fingerprints.First(f => f.Sql == "SELECT * FROM Users");
        var ordersFingerprint = fingerprints.First(f => f.Sql == "SELECT * FROM Orders");

        Assert.Equal(2, usersFingerprint.SampleCount);
        Assert.Equal(125, usersFingerprint.AverageDuration.TotalMilliseconds, 1); // (100+150)/2
        Assert.Equal(2, ordersFingerprint.SampleCount);
        Assert.Equal(250, ordersFingerprint.AverageDuration.TotalMilliseconds, 1); // (200+300)/2
    }

    /// <summary>
    /// Verifies that GetFingerprints computes all required statistics correctly.
    /// </summary>
    [Fact]
    public void GetFingerprints_computes_all_statistics()
    {
        var ranking = new SlowQueryRanking(10);

        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(500, "Query1"));
        ranking.Add(Sample(300, "Query1"));
        ranking.Add(Sample(200, "Query1"));

        var fingerprints = ranking.GetFingerprints();
        Assert.Single(fingerprints);

        var f = fingerprints[0];
        Assert.Equal(4, f.SampleCount);
        Assert.Equal(275, f.AverageDuration.TotalMilliseconds, 1); // (100+500+300+200)/4
        Assert.Equal(500, f.MaxDuration.TotalMilliseconds);
        Assert.Equal(100, f.MinDuration.TotalMilliseconds);
        Assert.Equal(1100, f.TotalDuration.TotalMilliseconds); // 100+500+300+200
        Assert.True(f.Percentile50.TotalMilliseconds > 0);
        Assert.True(f.Percentile95.TotalMilliseconds > 0);
        Assert.True(f.Percentile99.TotalMilliseconds > 0);
    }

    /// <summary>
    /// Verifies that Clear removes all samples.
    /// </summary>
    [Fact]
    public void Clear_removes_all_samples()
    {
        var ranking = new SlowQueryRanking(10);
        ranking.Add(Sample(100));
        ranking.Add(Sample(200));

        Assert.Equal(2, ranking.Count);

        ranking.Clear();

        Assert.Equal(0, ranking.Count);
        Assert.Empty(ranking.Snapshot());
    }

    /// <summary>
    /// Verifies tie behavior - when samples have equal durations.
    /// </summary>
    [Fact]
    public void Handles_ties_in_duration()
    {
        var ranking = new SlowQueryRanking(10);

        // All samples have the same duration
        ranking.Add(Sample(100, "Query1"));
        ranking.Add(Sample(100, "Query2"));
        ranking.Add(Sample(100, "Query3"));

        var snap = ranking.Snapshot();
        Assert.Equal(3, snap.Count);
        // All samples should be present
        Assert.Contains(snap, s => s.Sql == "Query1");
        Assert.Contains(snap, s => s.Sql == "Query2");
        Assert.Contains(snap, s => s.Sql == "Query3");
    }

    /// <summary>
    /// Verifies that top-N truncation works correctly with various capacities.
    /// </summary>
    [Fact]
    public void Top_n_truncation_works_with_various_capacities()
    {
        // Test with capacity of 1
        var ranking1 = new SlowQueryRanking(1);
        ranking1.Add(Sample(100));
        ranking1.Add(Sample(200));
        ranking1.Add(Sample(50));
        Assert.Single(ranking1.Snapshot());
        Assert.Equal(200, ranking1.Snapshot()[0].Duration.TotalMilliseconds);

        // Test with capacity of 5
        var ranking5 = new SlowQueryRanking(5);
        for (int i = 1; i <= 10; i++)
        {
            ranking5.Add(Sample(i * 100));
        }
        Assert.Equal(5, ranking5.Snapshot().Count);
        Assert.Equal(1000, ranking5.Snapshot()[0].Duration.TotalMilliseconds); // 1000ms
        Assert.Equal(600, ranking5.Snapshot()[4].Duration.TotalMilliseconds); // 600ms
    }

    /// <summary>
    /// Verifies thread safety by adding samples from multiple threads.
    /// </summary>
    [Fact]
    public void Is_thread_safe()
    {
        var ranking = new SlowQueryRanking(50);
        var tasks = new List<Task>();

        // Add samples from 4 different threads
        for (int i = 0; i < 4; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 25; j++)
                {
                    ranking.Add(Sample(100 + threadId * 10 + j, $"Query{threadId}"));
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Should have samples from all threads
        var snap = ranking.Snapshot();
        Assert.Equal(50, snap.Count); // Should be at capacity
        Assert.True(snap[0].Duration.TotalMilliseconds >= 124); // Highest duration
    }
}
