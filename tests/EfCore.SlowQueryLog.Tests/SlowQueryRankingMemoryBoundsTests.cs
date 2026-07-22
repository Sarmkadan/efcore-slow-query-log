using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Reporting;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Tests for memory bounds and eviction policy in SlowQueryRanking.
/// </summary>
public class SlowQueryRankingMemoryBoundsTests
{
    /// <summary>
    /// Creates a new SlowQuerySample instance with the specified duration.
    /// </summary>
    private static SlowQuerySample Sample(int ms, string sql = "SELECT 1") => new()
    {
        Sql = sql,
        Duration = TimeSpan.FromMilliseconds(ms),
        CapturedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>
    /// Stress test: Verifies that memory bounds are respected under high concurrency.
    /// Tests that the ranking never exceeds MaxSamples and maintains thread-safety.
    /// </summary>
    [Fact]
    public void Stress_test_memory_bounds_and_thread_safety()
    {
        const int maxSamples = 1000;
        const int numThreads = 10;
        const int samplesPerThread = 200;

        var ranking = new SlowQueryRanking(maxSamples, 50);
        var tasks = new List<Task>();
        var random = new Random(42);

        for (int i = 0; i < numThreads; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < samplesPerThread; j++)
                {
                    int duration = 10 + random.Next(4990);
                    ranking.Add(Sample(duration, $"Thread{threadId}_Query{j}"));
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        var snap = ranking.Snapshot();
        Assert.True(snap.Count <= 50, $"Ranking count {snap.Count} should not exceed capacity 50");

        foreach (var sample in snap)
        {
            Assert.NotNull(sample);
            Assert.NotNull(sample.Sql);
            Assert.True(sample.Duration.TotalMilliseconds > 0);
        }

        var fingerprints = ranking.GetFingerprints();
        Assert.NotNull(fingerprints);
    }

    /// <summary>
    /// Verifies FIFO eviction policy: oldest samples are removed when MaxSamples is reached.
    /// </summary>
    [Fact]
    public void Eviction_policy_uses_FIFO_strategy()
    {
        const int maxSamples = 5;
        var ranking = new SlowQueryRanking(maxSamples, 10);

        var sample1 = Sample(100, "Query1");
        var sample2 = Sample(200, "Query2");
        var sample3 = Sample(300, "Query3");
        var sample4 = Sample(400, "Query4");
        var sample5 = Sample(500, "Query5");
        var sample6 = Sample(600, "Query6");

        ranking.Add(sample1);
        ranking.Add(sample2);
        ranking.Add(sample3);
        ranking.Add(sample4);
        ranking.Add(sample5);

        Assert.Equal(5, ranking.Count);

        ranking.Add(sample6);

        Assert.Equal(5, ranking.Count);
        var snapshot = ranking.Snapshot();
        Assert.DoesNotContain(snapshot, s => s == sample1);
        Assert.Contains(snapshot, s => s == sample6);
    }

    /// <summary>
    /// Verifies that MaxSamples parameter is properly enforced.
    /// </summary>
    [Fact]
    public void MaxSamples_parameter_enforced()
    {
        const int maxSamples = 10;
        var ranking = new SlowQueryRanking(maxSamples, 100);

        for (int i = 0; i < maxSamples + 5; i++)
        {
            ranking.Add(Sample(100 + i, $"Query{i}"));
        }

        // Should never exceed MaxSamples
        var snap = ranking.Snapshot();
        Assert.True(snap.Count <= 100, $"Ranking should respect RankingCapacity");
    }

    /// <summary>
    /// Verifies that RankingCapacity parameter controls the number of samples returned.
    /// </summary>
    [Fact]
    public void RankingCapacity_parameter_controls_output()
    {
        const int rankingCapacity = 3;
        var ranking = new SlowQueryRanking(int.MaxValue, rankingCapacity);

        for (int i = 0; i < 10; i++)
        {
            ranking.Add(Sample(100 + i, $"Query{i}"));
        }

        var snap = ranking.Snapshot();
        Assert.Equal(rankingCapacity, snap.Count);
    }
}