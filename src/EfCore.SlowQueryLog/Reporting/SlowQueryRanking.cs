namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Thread-safe, bounded ranking of the slowest queries observed so far, ordered by
/// duration descending. Keeps at most <c>capacity</c> entries.
/// </summary>
public sealed class SlowQueryRanking
{
    private readonly object _gate = new();
    private readonly List<SlowQuerySample> _items = new();
    private readonly int _capacity;

    public SlowQueryRanking(int capacity)
    {
        if (capacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(capacity));
        _capacity = capacity;
    }

    public void Add(SlowQuerySample sample)
    {
        lock (_gate)
        {
            _items.Add(sample);
            _items.Sort(static (a, b) => b.Duration.CompareTo(a.Duration));
            if (_items.Count > _capacity)
                _items.RemoveRange(_capacity, _items.Count - _capacity);
        }
    }

    /// <summary>Returns a snapshot of the current ranking, slowest first.</summary>
    public IReadOnlyList<SlowQuerySample> Snapshot()
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
        lock (_gate) _items.Clear();
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

            foreach (var sample in _items)
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
