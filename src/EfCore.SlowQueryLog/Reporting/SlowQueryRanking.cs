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
            var groups = _items
                .GroupBy(s => s.Sql)
                .Select(g => new SlowQueryFingerprint(g.Key, g.FirstOrDefault()?.Parameters, g.FirstOrDefault()?.Suggestions ?? Array.Empty<IndexSuggestion>()))
                .ToList();

            // Compute statistics for each group
            foreach (var fingerprint in groups)
            {
                var groupSamples = _items.Where(s => s.Sql == fingerprint.Sql).ToList();
                fingerprint.SampleCount = groupSamples.Count;
                fingerprint.AverageDuration = groupSamples.Count == 0 ? TimeSpan.Zero : TimeSpan.FromMilliseconds(groupSamples.Average(s => s.Duration.TotalMilliseconds));
                fingerprint.MaxDuration = groupSamples.Max(s => s.Duration);
                fingerprint.MinDuration = groupSamples.Min(s => s.Duration);
                fingerprint.TotalDuration = TimeSpan.FromMilliseconds(groupSamples.Sum(s => s.Duration.TotalMilliseconds));

                // Compute P95 from all durations in this group
                var durations = groupSamples.Select(s => s.Duration).ToList();
                fingerprint.ComputePercentile95(durations);
            }

            // Sort by average duration descending
            return groups.OrderByDescending(f => f.AverageDuration).ToList();
        }
    }
}
