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
}
