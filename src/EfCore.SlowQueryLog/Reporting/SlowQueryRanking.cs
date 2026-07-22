using System;
using System.Collections.Generic;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Thread-safe, bounded ranking of the slowest queries observed so far, ordered by
/// duration descending. Keeps at most <c>capacity</c> entries for ranking display,
/// while maintaining a separate <c>maxSamples</c> limit for the total sample store.
/// </summary>
public sealed class SlowQueryRanking
{
    private readonly object _gate = new();
    private readonly List<SlowQuerySample> _allSamples = new();
    private readonly List<SlowQuerySample> _rankedSamples;
    private readonly int _maxSamples;
    private readonly int _rankingCapacity;

    public SlowQueryRanking(int capacity)
        : this(int.MaxValue, capacity)
    {
    }

    public SlowQueryRanking(int maxSamples, int rankingCapacity = 25)
    {
        if (maxSamples <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxSamples));
        if (rankingCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(rankingCapacity));

        _maxSamples = maxSamples;
        _rankingCapacity = rankingCapacity;
        _rankedSamples = new List<SlowQuerySample>(rankingCapacity);
    }

    public void Add(SlowQuerySample sample)
    {
        lock (_gate)
        {
            // If we've reached maxSamples, remove the slowest sample to make room
            // This ensures we always keep the slowest queries in memory
            if (_allSamples.Count >= _maxSamples)
            {
                // Find and remove the slowest sample (highest duration)
                int slowestIndex = 0;
                for (int i = 1; i < _allSamples.Count; i++)
                {
                    if (_allSamples[i].Duration > _allSamples[slowestIndex].Duration)
                    {
                        slowestIndex = i;
                    }
                }
                _allSamples.RemoveAt(slowestIndex);
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
