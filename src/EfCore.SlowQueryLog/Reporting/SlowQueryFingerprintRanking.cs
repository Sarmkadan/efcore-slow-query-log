using System;
using System.Collections.Generic;
using System.Linq;
using EfCore.SlowQueryLog.Analysis;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Thread-safe, bounded ranking of slow query fingerprints (grouped by SQL), ordered by
/// specified metric. Keeps at most <c>capacity</c> entries.
/// </summary>
public sealed class SlowQueryFingerprintRanking : ISlowQueryRanking
{
    private readonly object _gate = new();
    private readonly List<SlowQueryFingerprint> _items = new();
    private readonly int _capacity;
    private readonly RankingMetric _metric;

    public enum RankingMetric
    {
        /// <summary>Order by average duration (default)</summary>
        AverageDuration,

        /// <summary>Order by total cumulative duration</summary>
        TotalDuration,

        /// <summary>Order by P95 duration</summary>
        P95Duration,

        /// <summary>Order by max duration</summary>
        MaxDuration
    }

    public SlowQueryFingerprintRanking(int capacity, RankingMetric metric = RankingMetric.AverageDuration)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
        _metric = metric;
    }

    public void Add(SlowQuerySample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        lock (_gate)
        {
            // Find existing fingerprint without creating intermediate dictionary
            SlowQueryFingerprint? existing = null;
            var existingIndex = -1;

            for (var i = 0; i < _items.Count; i++)
            {
                if (string.Equals(_items[i].Sql, sample.Sql, StringComparison.Ordinal))
                {
                    existing = _items[i];
                    existingIndex = i;
                    break;
                }
            }

            if (existing != null)
            {
                // Update existing fingerprint with new sample
                existing.AddSample(sample);
            }
            else
            {
                // Add new fingerprint
                _items.Add(new SlowQueryFingerprint(sample.Sql, sample.Parameters, sample.Suggestions));
            }

            // Sort by the selected metric
            SortItems();

            // Keep only top items
            if (_items.Count > _capacity)
                _items.RemoveRange(_capacity, _items.Count - _capacity);
        }
    }

    public void AddRange(IEnumerable<SlowQuerySample> samples)
    {
        lock (_gate)
        {
            foreach (var sample in samples)
            {
                Add(sample);
            }
        }
    }

    private void SortItems()
    {
        _items.Sort(GetComparison());
    }

    private Comparison<SlowQueryFingerprint> GetComparison()
    {
        return _metric switch
        {
            RankingMetric.TotalDuration => (a, b) => b.TotalDuration.CompareTo(a.TotalDuration),
            RankingMetric.P95Duration => (a, b) => b.Percentile95.CompareTo(a.Percentile95),
            RankingMetric.MaxDuration => (a, b) => b.MaxDuration.CompareTo(a.MaxDuration),
            _ => (a, b) => b.AverageDuration.CompareTo(a.AverageDuration) // Default to AverageDuration
        };
    }

    /// <summary>Returns a snapshot of the current ranking, highest ranked first.</summary>
    public IReadOnlyList<SlowQueryFingerprint> Snapshot()
    {
        lock (_gate)
            return _items.ToArray();
    }

    public int Count
    {
        get { lock (_gate) return _items.Count; }
    }

    public void Clear()
    {
        lock (_gate)
            _items.Clear();
    }

    public RankingMetric Metric
    {
        get { lock (_gate) return _metric; }
    }

    /// <summary>
    /// Records a slow query sample into the ranking. Equivalent to <see cref="Add(SlowQuerySample)"/>;
    /// provided to satisfy <see cref="ISlowQueryRanking"/>.
    /// </summary>
    /// <param name="sample">The slow query sample to record.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sample"/> is null.</exception>
    public void Record(SlowQuerySample sample) => Add(sample);

    /// <summary>
    /// Returns the top <paramref name="count"/> fingerprints from this ranking, ordered by the configured <see cref="Metric"/>.
    /// </summary>
    /// <param name="count">The maximum number of fingerprints to return.</param>
    /// <returns>A list of at most <paramref name="count"/> fingerprints.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is negative.</exception>
    public IReadOnlyList<SlowQueryFingerprint> TopN(int count)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        lock (_gate)
            return _items.Take(count).ToList();
    }

    /// <summary>
    /// Gets the total duration attributed to all fingerprints tracked by this ranking, computed as
    /// the sum of each fingerprint's <see cref="SlowQueryFingerprint.TotalDuration"/>.
    /// </summary>
    public TimeSpan TotalDuration
    {
        get
        {
            lock (_gate)
            {
                double totalMs = 0;
                foreach (var item in _items)
                    totalMs += item.TotalDuration.TotalMilliseconds;
                return TimeSpan.FromMilliseconds(totalMs);
            }
        }
    }

    /// <summary>
    /// Gets the average duration, in milliseconds, across all samples represented by the tracked fingerprints
    /// (weighted by each fingerprint's sample count). Returns 0.0 when the ranking is empty.
    /// </summary>
    public double AverageDurationMs
    {
        get
        {
            lock (_gate)
            {
                var totalSamples = 0;
                var totalMs = 0.0;
                foreach (var item in _items)
                {
                    totalSamples += item.SampleCount;
                    totalMs += item.TotalDuration.TotalMilliseconds;
                }
                return totalSamples == 0 ? 0.0 : totalMs / totalSamples;
            }
        }
    }

    /// <summary>
    /// Gets all aggregated index suggestions collected across the tracked fingerprints, deduplicated and
    /// ranked by total attributed duration. Since this ranking does not retain individual samples, each
    /// fingerprint's suggestions are attributed using its cumulative <see cref="SlowQueryFingerprint.TotalDuration"/>.
    /// </summary>
    /// <returns>An enumerable of aggregated index suggestions with statistics.</returns>
    public IEnumerable<IndexSuggestionAggregator.AggregatedIndexSuggestion> GetAllSuggestions()
    {
        var aggregator = new IndexSuggestionAggregator();

        lock (_gate)
        {
            foreach (var item in _items)
            {
                if (item.Suggestions.Count > 0)
                    aggregator.Add(item.Suggestions, item.TotalDuration);
            }
        }

        return aggregator.AllSuggestions();
    }
}

/// <summary>
/// Aggregated statistics for a query fingerprint (grouped by SQL).
/// </summary>
public sealed class SlowQueryFingerprint
{
    public string Sql { get; set; } = string.Empty;

    public string? Parameters { get; set; }

    public IReadOnlyList<IndexSuggestion> Suggestions { get; set; } = Array.Empty<IndexSuggestion>();

    public int SampleCount { get; set; }
    public TimeSpan AverageDuration { get; set; }
    public TimeSpan MaxDuration { get; set; }
    public TimeSpan MinDuration { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public TimeSpan Percentile50 { get; set; }
    public TimeSpan Percentile95 { get; set; }
    public TimeSpan Percentile99 { get; set; }

    public SlowQueryFingerprint()
    {
        // Initialize with zero values
        AverageDuration = TimeSpan.Zero;
        MaxDuration = TimeSpan.Zero;
        MinDuration = TimeSpan.MaxValue;
        TotalDuration = TimeSpan.Zero;
        Percentile50 = TimeSpan.Zero;
        Percentile95 = TimeSpan.Zero;
        Percentile99 = TimeSpan.Zero;
    }

    public SlowQueryFingerprint(string sql, string? parameters, IReadOnlyList<IndexSuggestion> suggestions)
    {
        Sql = sql;
        Parameters = parameters;
        Suggestions = suggestions;

        // Initialize with zero values
        AverageDuration = TimeSpan.Zero;
        MaxDuration = TimeSpan.Zero;
        MinDuration = TimeSpan.MaxValue;
        TotalDuration = TimeSpan.Zero;
        Percentile50 = TimeSpan.Zero;
        Percentile95 = TimeSpan.Zero;
        Percentile99 = TimeSpan.Zero;
    }

    public void AddSample(SlowQuerySample sample)
    {
        SampleCount++;
        var durationMs = sample.Duration.TotalMilliseconds;

        // Update min/max
        if (sample.Duration < MinDuration)
            MinDuration = sample.Duration;
        if (sample.Duration > MaxDuration)
            MaxDuration = sample.Duration;

        // Update total
        TotalDuration = TimeSpan.FromMilliseconds(TotalDuration.TotalMilliseconds + durationMs);

        // Update average
        AverageDuration = TimeSpan.FromMilliseconds(TotalDuration.TotalMilliseconds / SampleCount);

        // Note: Percentiles are computed separately via ComputePercentiles method
    }

    /// <summary>
    /// Computes P50, P95, and P99 from the collected samples.
    /// </summary>
    public void ComputePercentiles(List<TimeSpan> allDurations)
    {
        if (allDurations.Count == 0)
        {
            Percentile50 = TimeSpan.Zero;
            Percentile95 = TimeSpan.Zero;
            Percentile99 = TimeSpan.Zero;
            return;
        }

        // Sort durations - use Array.Sort for better performance with List<T>
        allDurations.Sort(static (a, b) => a.CompareTo(b));

        Percentile50 = GetPercentile(allDurations, 0.50);
        Percentile95 = GetPercentile(allDurations, 0.95);
        Percentile99 = GetPercentile(allDurations, 0.99);
    }

    private static TimeSpan GetPercentile(List<TimeSpan> sortedDurations, double percentile)
    {
        // Optimized percentile calculation without bounds checks in hot path
        int index = (int)Math.Ceiling(sortedDurations.Count * percentile) - 1;
        if ((uint)index >= (uint)sortedDurations.Count) // Use uint comparison for bounds check
            index = index < 0 ? 0 : sortedDurations.Count - 1;
        return sortedDurations[index];
    }
}