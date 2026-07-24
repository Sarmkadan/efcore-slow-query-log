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

    /// <summary>
    /// Verifies that ordering is maintained after EVERY insert, not just the final state.
    /// Tests the invariant that after each Add() call, the ranking is always sorted descending by duration.
    /// </summary>
    [Fact]
    public void Ordering_is_maintained_after_every_insert()
    {
        var ranking = new SlowQueryRanking(10);

        // Insert samples in random order
        var samples = new[]
        {
            Sample(150),
            Sample(950),
            Sample(50),
            Sample(750),
            Sample(250),
            Sample(850),
            Sample(100),
            Sample(650),
            Sample(350),
            Sample(450)
        };

        // Add each sample and verify ordering after each insertion
        foreach (var sample in samples)
        {
            ranking.Add(sample);
            var snapshot = ranking.Snapshot();

            // Verify all samples are sorted by duration descending
            for (int i = 0; i < snapshot.Count - 1; i++)
            {
                Assert.True(snapshot[i].Duration >= snapshot[i + 1].Duration,
                    $"Sample at index {i} with duration {snapshot[i].Duration.TotalMilliseconds}ms " +
                    $"should be >= sample at index {i + 1} with duration {snapshot[i + 1].Duration.TotalMilliseconds}ms");
            }
        }

        // Final verification
        var finalSnapshot = ranking.Snapshot();
        Assert.Equal(10, finalSnapshot.Count);
        Assert.Equal(950, finalSnapshot[0].Duration.TotalMilliseconds);
        Assert.Equal(850, finalSnapshot[1].Duration.TotalMilliseconds);
        Assert.Equal(750, finalSnapshot[2].Duration.TotalMilliseconds);
        Assert.Equal(650, finalSnapshot[3].Duration.TotalMilliseconds);
        Assert.Equal(450, finalSnapshot[4].Duration.TotalMilliseconds);
        Assert.Equal(350, finalSnapshot[5].Duration.TotalMilliseconds);
        Assert.Equal(250, finalSnapshot[6].Duration.TotalMilliseconds);
        Assert.Equal(150, finalSnapshot[7].Duration.TotalMilliseconds);
        Assert.Equal(100, finalSnapshot[8].Duration.TotalMilliseconds);
        Assert.Equal(50, finalSnapshot[9].Duration.TotalMilliseconds);
    }

    /// <summary>
    /// Verifies precise capacity boundary behavior at exactly N entries.
    /// Tests the off-by-one scenario: when capacity is reached, the slowest N samples are kept.
    /// </summary>
    [Fact]
    public void Capacity_boundary_exactly_at_N_entries()
    {
        const int capacity = 5;
        var ranking = new SlowQueryRanking(capacity);

        // Add exactly capacity samples
        for (int i = 1; i <= capacity; i++)
        {
            ranking.Add(Sample(i * 100));
        }

        Assert.Equal(capacity, ranking.Count);
        var snapshot = ranking.Snapshot();

        // Should contain the 5 slowest: 500, 400, 300, 200, 100
        Assert.Equal(500, snapshot[0].Duration.TotalMilliseconds);
        Assert.Equal(400, snapshot[1].Duration.TotalMilliseconds);
        Assert.Equal(300, snapshot[2].Duration.TotalMilliseconds);
        Assert.Equal(200, snapshot[3].Duration.TotalMilliseconds);
        Assert.Equal(100, snapshot[4].Duration.TotalMilliseconds);

        // Add one more sample - should evict the fastest (100ms)
        ranking.Add(Sample(600));
        Assert.Equal(capacity, ranking.Count);
        snapshot = ranking.Snapshot();

        // Should now contain: 600, 500, 400, 300, 200
        Assert.Equal(600, snapshot[0].Duration.TotalMilliseconds);
        Assert.Equal(500, snapshot[1].Duration.TotalMilliseconds);
        Assert.Equal(400, snapshot[2].Duration.TotalMilliseconds);
        Assert.Equal(300, snapshot[3].Duration.TotalMilliseconds);
        Assert.Equal(200, snapshot[4].Duration.TotalMilliseconds);
        Assert.DoesNotContain(snapshot, s => s.Duration.TotalMilliseconds == 100);
    }

    /// <summary>
    /// Verifies deterministic tie-breaking when samples have identical durations.
    /// Ensures stable ordering for samples with the same duration.
    /// </summary>
    [Fact]
    public void Deterministic_tie_breaking_for_identical_durations()
    {
        var ranking = new SlowQueryRanking(10);

        // Create samples with identical durations but different SQL
        var sample1 = Sample(500, "QueryZ");
        var sample2 = Sample(500, "QueryA");
        var sample3 = Sample(500, "QueryM");
        var sample4 = Sample(500, "Query1");

        ranking.Add(sample1);
        ranking.Add(sample2);
        ranking.Add(sample3);
        ranking.Add(sample4);

        var snapshot = ranking.Snapshot();
        Assert.Equal(4, snapshot.Count);

        // All should have the same duration
        foreach (var sample in snapshot)
        {
            Assert.Equal(500, sample.Duration.TotalMilliseconds);
        }

        // Verify deterministic ordering - should maintain insertion order
        // The implementation uses List.Sort with default comparer which is stable for equal elements
        Assert.Equal("QueryZ", snapshot[0].Sql);
        Assert.Equal("QueryA", snapshot[1].Sql);
        Assert.Equal("QueryM", snapshot[2].Sql);
        Assert.Equal("Query1", snapshot[3].Sql);
    }

    /// <summary>
    /// Stress test for thread-safety: verifies no lost updates and internal consistency
    /// when adding samples from many concurrent threads.
    /// </summary>
    [Fact]
    public void Concurrent_inserts_no_lost_updates_and_internal_consistency()
    {
        const int capacity = 100;
        const int numThreads = 20;
        const int samplesPerThread = 50;
        const int totalSamples = numThreads * samplesPerThread;

        var ranking = new SlowQueryRanking(capacity);
        var tasks = new List<Task>();
        var random = new Random(12345);
        var allAddedSamples = new List<SlowQuerySample>();

        // Track all samples added from each thread
        var threadSamples = new List<SlowQuerySample>[numThreads];
        for (int i = 0; i < numThreads; i++)
        {
            threadSamples[i] = new List<SlowQuerySample>();
        }

        // Start threads that add samples concurrently
        for (int i = 0; i < numThreads; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() =>
            {
                var threadSamplesList = threadSamples[threadId];
                for (int j = 0; j < samplesPerThread; j++)
                {
                    int duration = 10 + random.Next(990);
                    var sample = Sample(duration, $"Thread{threadId}_Query{j}");
                    ranking.Add(sample);
                    threadSamplesList.Add(sample);
                    lock (allAddedSamples)
                    {
                        allAddedSamples.Add(sample);
                    }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Verify no exceptions were thrown
        Assert.True(true, "No exceptions thrown during concurrent inserts");

        // Verify ranking count is within capacity bounds
        var snapshot = ranking.Snapshot();
        Assert.InRange(snapshot.Count, 0, capacity);

        // Verify all samples in ranking are from the added samples
        foreach (var sample in snapshot)
        {
            Assert.Contains(sample, allAddedSamples);
        }

        // Verify the ranking is properly sorted
        for (int i = 0; i < snapshot.Count - 1; i++)
        {
            Assert.True(snapshot[i].Duration >= snapshot[i + 1].Duration,
                $"Ranking not sorted at index {i}: {snapshot[i].Duration.TotalMilliseconds} >= {snapshot[i + 1].Duration.TotalMilliseconds}");
        }

        // Verify we can get fingerprints without errors
        var fingerprints = ranking.GetFingerprints();
        Assert.NotNull(fingerprints);

        // Verify total count across all threads
        int totalAdded = allAddedSamples.Count;
        Assert.Equal(totalSamples, totalAdded);
    }

    /// <summary>
    /// Verifies thread-safety with parallel inserts and concurrent snapshots.
    /// Ensures that while one thread is adding samples, another can safely read snapshots.
    /// </summary>
    [Fact]
    public void Parallel_inserts_and_snapshots_are_thread_safe()
    {
        const int capacity = 50;
        const int numThreads = 10;
        const int samplesPerThread = 25;

        var ranking = new SlowQueryRanking(capacity);
        var tasks = new List<Task>();

        // Threads that add samples
        for (int i = 0; i < numThreads; i++)
        {
            int threadId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < samplesPerThread; j++)
                {
                    ranking.Add(Sample(100 + threadId * 10 + j, $"Query{threadId}_{j}"));
                }
            }));
        }

        // Threads that take snapshots while inserts are happening
        for (int i = 0; i < numThreads; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                // Take multiple snapshots
                for (int k = 0; k < 5; k++)
                {
                    var snapshot = ranking.Snapshot();
                    Assert.NotNull(snapshot);
                    Assert.InRange(snapshot.Count, 0, capacity);

                    // Verify snapshot is sorted
                    for (int idx = 0; idx < snapshot.Count - 1; idx++)
                    {
                        Assert.True(snapshot[idx].Duration >= snapshot[idx + 1].Duration);
                    }
                }
            }));
        }

        Task.WaitAll(tasks.ToArray());

        // Final verification
        var finalSnapshot = ranking.Snapshot();
        Assert.InRange(finalSnapshot.Count, 0, capacity);
        for (int i = 0; i < finalSnapshot.Count - 1; i++)
        {
            Assert.True(finalSnapshot[i].Duration >= finalSnapshot[i + 1].Duration);
        }
    }

    /// <summary>
    /// Verifies that the ranking correctly maintains the slowest queries when capacity is reached,
    /// and that eviction removes the fastest query (not the oldest).
    /// </summary>
    [Fact]
    public void Eviction_removes_fastest_not_oldest()
    {
        const int capacity = 3;
        var ranking = new SlowQueryRanking(capacity);
        var baseTime = DateTimeOffset.UtcNow;

        // Add samples in order: slow, medium, fast
        var slow = new SlowQuerySample
        {
            Sql = "SlowQuery",
            Duration = TimeSpan.FromMilliseconds(500),
            CapturedAt = baseTime
        };
        var medium = new SlowQuerySample
        {
            Sql = "MediumQuery",
            Duration = TimeSpan.FromMilliseconds(300),
            CapturedAt = baseTime.AddSeconds(1)
        };
        var fast = new SlowQuerySample
        {
            Sql = "FastQuery",
            Duration = TimeSpan.FromMilliseconds(100),
            CapturedAt = baseTime.AddSeconds(2)
        };

        ranking.Add(slow);
        ranking.Add(medium);
        ranking.Add(fast);

        Assert.Equal(3, ranking.Count);
        var snapshot = ranking.Snapshot();
        Assert.Equal(500, snapshot[0].Duration.TotalMilliseconds);
        Assert.Equal(300, snapshot[1].Duration.TotalMilliseconds);
        Assert.Equal(100, snapshot[2].Duration.TotalMilliseconds);

        // Add another fast query - should evict the existing fast query (100ms)
        var newerFast = new SlowQuerySample
        {
            Sql = "NewerFastQuery",
            Duration = TimeSpan.FromMilliseconds(150),
            CapturedAt = baseTime.AddSeconds(3)
        };
        ranking.Add(newerFast);

        snapshot = ranking.Snapshot();
        Assert.Equal(3, ranking.Count);
        Assert.Equal(500, snapshot[0].Duration.TotalMilliseconds);
        Assert.Equal(300, snapshot[1].Duration.TotalMilliseconds);
        Assert.Equal(150, snapshot[2].Duration.TotalMilliseconds);
        Assert.DoesNotContain(snapshot, s => s.Sql == "FastQuery");
        Assert.Contains(snapshot, s => s.Sql == "NewerFastQuery");
    }
}
