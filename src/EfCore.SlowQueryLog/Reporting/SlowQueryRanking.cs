using System;
using System.Collections.Generic;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Thread-safe, bounded ranking of the slowest queries observed so far, ordered by
/// duration descending. Keeps at most <see cref="RankingCapacity"/> entries for ranking display,
/// while maintaining a separate <see cref="MaxSamples"/> limit for the total sample store.
///
/// <para>
/// Memory is bounded using a two-tier strategy:
/// <list type="bullet">
/// <item><description><see cref="MaxSamples"/>: Maximum total samples retained in memory. When this limit is reached,
/// the oldest samples are evicted using a FIFO strategy to prevent unbounded memory growth.
/// This ensures the system can handle applications with many distinct SQL texts (dynamic IN-lists,
/// unparameterized literals) without unbounded memory consumption.</description></item>
/// <item><description><see cref="RankingCapacity"/>: Maximum samples kept for ranking display. When this limit is reached,
/// only the slowest queries are retained. This is separate from <see cref="MaxSamples"/> and controls
/// the size of the ranked results displayed in reports and dashboards.</description></item>
/// </list>
/// </para>
///
/// <para>
/// Thread-safety: All public methods are thread-safe. Concurrent calls to <see cref="Add(SlowQuerySample)"/>
/// from multiple <see cref="global::System.Data.Common.DbContext"/> instances are safe. The implementation uses a single <c>lock</c>
/// object (<see cref="_gate"/>) to synchronize access to internal collections.
/// </para>
/// </summary>
public sealed class SlowQueryRanking
{
    /// <summary>
    /// Synchronization gate for thread-safe access to internal collections.
    /// </summary>
    private readonly object _gate = new();

    /// <summary>
    /// All captured samples, bounded by <see cref="MaxSamples"/>. Uses FIFO eviction when full.
    /// </summary>
    private readonly List<SlowQuerySample> _allSamples;

    /// <summary>
    /// Top N samples for ranking display, bounded by <see cref="RankingCapacity"/>.
    /// Always contains the slowest queries from <see cref="_allSamples"/>.
    /// </summary>
    private readonly List<SlowQuerySample> _rankedSamples;

    /// <summary>
    /// Maximum number of samples to retain in memory. When this limit is reached,
    /// the oldest sample is evicted using FIFO strategy.
    /// </summary>
    private readonly int _maxSamples;

    /// <summary>
    /// Maximum number of samples to retain for ranking display. Controls the size
    /// of the ranked results returned by <see cref="Snapshot()"/>.
    /// </summary>
    private readonly int _rankingCapacity;

    public SlowQueryRanking(int capacity)
        : this(int.MaxValue, capacity)
    {
    }

    public SlowQueryRanking(int maxSamples, int rankingCapacity = 25)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxSamples, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(rankingCapacity, 0);

        _maxSamples = maxSamples;
        _rankingCapacity = rankingCapacity;
        _allSamples = new List<SlowQuerySample>(Math.Min(maxSamples, 1000));
        _rankedSamples = new List<SlowQuerySample>(rankingCapacity);
    }

    /// <summary>
    /// Adds a slow query sample to the ranking.
    /// </summary>
    /// <param name="sample">The slow query sample to add. Cannot be null.</param>
    /// <exception cref="ArgumentNullException"><paramref name="sample"/> is null.</exception>
    /// <remarks>
    /// <para>
    /// Memory management: When the total number of samples reaches <see cref="MaxSamples"/>, the oldest
    /// sample (by insertion order) is evicted using a FIFO strategy. This ensures predictable memory
    /// usage even when the application generates many distinct SQL texts.
    /// </para>
    /// <para>
    /// Thread-safety: This method is thread-safe and can be called concurrently from multiple
    /// <see cref="DbContext"/> instances.
    /// </para>
    /// </remarks>
    public void Add(SlowQuerySample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        lock (_gate)
        {
            // If we've reached maxSamples, evict the oldest sample using FIFO strategy
            // This ensures we keep the most recent samples, which are typically more relevant
            if (_allSamples.Count >= _maxSamples)
            {
                _allSamples.RemoveAt(0); // Remove oldest (FIFO)
            }

            _allSamples.Add(sample);

            // Maintain the ranked samples (top N by duration)
            UpdateRankedSamples();
        }
    }

    private void UpdateRankedSamples()
    {
        // Create a working copy of all samples for ranking
        var samplesToRank = new List<SlowQuerySample>(_allSamples);

        // Sort by duration descending to get the slowest queries
        samplesToRank.Sort(static (a, b) => b.Duration.CompareTo(a.Duration));

        // Keep only the top N samples for ranking display
        if (samplesToRank.Count > _rankingCapacity)
        {
            samplesToRank.RemoveRange(_rankingCapacity, samplesToRank.Count - _rankingCapacity);
        }

        _rankedSamples.Clear();
        _rankedSamples.AddRange(samplesToRank);
    }

    /// <summary>Returns a snapshot of the current ranking, slowest first.</summary>
    public IReadOnlyList<SlowQuerySample> Snapshot()
    {
        lock (_gate)
            return _rankedSamples.ToArray();
    }

    public int Count
    {
        get
        {
            lock (_gate)
                return _rankedSamples.Count;
        }
    }

    public void Clear()
    {
        lock (_gate)
        {
            _allSamples.Clear();
            _rankedSamples.Clear();
        }
    }

    /// <summary>
    /// Gets the total duration of all captured slow queries.
    /// </summary>
    public TimeSpan GetTotalDuration()
    {
        lock (_gate)
        {
            double totalMs = 0;
            foreach (var sample in _allSamples)
            {
                totalMs += sample.Duration.TotalMilliseconds;
            }
            return TimeSpan.FromMilliseconds(totalMs);
        }
    }

    /// <summary>
    /// Gets the average duration of all captured slow queries in milliseconds.
    /// </summary>
    public double GetAverageDuration()
    {
        lock (_gate)
        {
            if (_allSamples.Count == 0)
                return 0.0;

            double totalMs = 0;
            foreach (var sample in _allSamples)
            {
                totalMs += sample.Duration.TotalMilliseconds;
            }
            return totalMs / _allSamples.Count;
        }
    }

    /// <summary>
    /// Gets all index suggestions from all captured queries.
    /// </summary>
    public IEnumerable<IndexSuggestion> GetAllSuggestions()
    {
        lock (_gate)
        {
            foreach (var sample in _allSamples)
            {
                if (sample.Suggestions != null)
                {
                    foreach (var suggestion in sample.Suggestions)
                    {
                        yield return suggestion;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Groups samples by SQL fingerprint and computes aggregated statistics (P95, max duration, etc.).
    /// </summary>
    /// <returns>A list of fingerprints with aggregated statistics, ordered by average duration descending.</returns>
    public IReadOnlyList<SlowQueryFingerprint> GetFingerprints()
    {
        lock (_gate)
        {
            // Group by SQL and collect all durations for P95 calculation
            // Use manual grouping to avoid LINQ intermediate allocations
            var groups = new List<SlowQueryFingerprint>();
            var groupMap = new Dictionary<string, List<SlowQuerySample>>(StringComparer.Ordinal);

            foreach (var sample in _allSamples)
            {
                if (!groupMap.TryGetValue(sample.Sql, out var groupList))
                {
                    groupList = new List<SlowQuerySample>();
                    groupMap[sample.Sql] = groupList;
                    groups.Add(new SlowQueryFingerprint(sample.Sql, sample.Parameters, sample.Suggestions ?? Array.Empty<IndexSuggestion>()));
                }
                groupList.Add(sample);
            }

            // Compute statistics for each group
            foreach (var fingerprint in groups)
            {
                if (groupMap.TryGetValue(fingerprint.Sql, out var groupSamples))
                {
                    fingerprint.SampleCount = groupSamples.Count;

                    if (groupSamples.Count > 0)
                    {
                        double totalMs = 0;
                        TimeSpan maxDuration = TimeSpan.MinValue;
                        TimeSpan minDuration = TimeSpan.MaxValue;
                        double totalSumMs = 0;

                        var durations = new List<TimeSpan>(groupSamples.Count);

                        foreach (var sample in groupSamples)
                        {
                            var durationMs = sample.Duration.TotalMilliseconds;
                            totalMs += durationMs;
                            totalSumMs += durationMs;

                            if (sample.Duration > maxDuration)
                                maxDuration = sample.Duration;

                            if (sample.Duration < minDuration)
                                minDuration = sample.Duration;

                            durations.Add(sample.Duration);
                        }

                        fingerprint.AverageDuration = TimeSpan.FromMilliseconds(totalMs / groupSamples.Count);
                        fingerprint.MaxDuration = maxDuration;
                        fingerprint.MinDuration = minDuration == TimeSpan.MaxValue ? TimeSpan.Zero : minDuration;
                        fingerprint.TotalDuration = TimeSpan.FromMilliseconds(totalSumMs);

                        // Compute P95 from all durations in this group
                        fingerprint.ComputePercentiles(durations);
                    }
                    else
                    {
                        fingerprint.AverageDuration = TimeSpan.Zero;
                        fingerprint.MaxDuration = TimeSpan.Zero;
                        fingerprint.MinDuration = TimeSpan.Zero;
                        fingerprint.TotalDuration = TimeSpan.Zero;
                        fingerprint.Percentile50 = TimeSpan.Zero;
                        fingerprint.Percentile95 = TimeSpan.Zero;
                        fingerprint.Percentile99 = TimeSpan.Zero;
                    }
                }
            }

            // Sort by average duration descending
            groups.Sort(static (a, b) => b.AverageDuration.CompareTo(a.AverageDuration));
            return groups;
        }
    }
}