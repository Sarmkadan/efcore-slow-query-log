using System;
using System.Collections.Generic;
using System.Linq;

namespace EfCore.SlowQueryLog.Analysis;

/// <summary>
/// Aggregates index suggestions across multiple slow query samples, providing deduplication,
/// confidence ranking, and aggregation of statistics (counts, total elapsed time).
/// </summary>
public sealed class IndexSuggestionAggregator
{
    private readonly Dictionary<IndexSuggestionKey, AggregatedSuggestion> _suggestions =
        new Dictionary<IndexSuggestionKey, AggregatedSuggestion>(IndexSuggestionKeyComparer.Instance);

    /// <summary>
    /// Adds a set of index suggestions from a slow query sample to the aggregator.
    /// </summary>
    /// <param name="suggestions">The index suggestions to aggregate.</param>
    /// <param name="duration">The duration of the query that produced these suggestions.</param>
    /// <exception cref="ArgumentNullException">Thrown if suggestions is null.</exception>
    public void Add(IReadOnlyList<IndexSuggestion> suggestions, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(suggestions);

        foreach (var suggestion in suggestions)
        {
            Add(suggestion, duration);
        }
    }

    /// <summary>
    /// Adds a single index suggestion to the aggregator.
    /// </summary>
    /// <param name="suggestion">The index suggestion to aggregate.</param>
    /// <param name="duration">The duration of the query that produced this suggestion.</param>
    /// <exception cref="ArgumentNullException">Thrown if suggestion is null.</exception>
    public void Add(IndexSuggestion suggestion, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(suggestion);

        // Create a key for structural comparison
        var key = new IndexSuggestionKey(suggestion);

        if (_suggestions.TryGetValue(key, out var existing))
        {
            // Update existing suggestion with aggregated statistics
            existing.Count++;
            existing.TotalDuration = existing.TotalDuration.Add(duration);

            // Merge include columns if they differ (take union)
            if (suggestion.IncludeColumns != null && suggestion.IncludeColumns.Count > 0)
            {
                if (existing.Suggestion.IncludeColumns == null)
                {
                    existing.Suggestion = suggestion with { IncludeColumns = suggestion.IncludeColumns };
                }
                else
                {
                    // Merge include columns
                    var mergedIncludes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    mergedIncludes.UnionWith(existing.Suggestion.IncludeColumns);
                    mergedIncludes.UnionWith(suggestion.IncludeColumns);
                    existing.Suggestion = suggestion with { IncludeColumns = mergedIncludes.ToArray() };
                }
            }
        }
        else
        {
            // Add new suggestion
            _suggestions[key] = new AggregatedSuggestion
            {
                Suggestion = suggestion,
                Count = 1,
                TotalDuration = duration
            };
        }
    }

    /// <summary>
    /// Gets the top N aggregated index suggestions ordered by total attributed duration (highest first).
    /// </summary>
    /// <param name="count">The maximum number of suggestions to return.</param>
    /// <returns>An enumerable of aggregated index suggestions.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if count is negative.</exception>
    public IEnumerable<AggregatedIndexSuggestion> TopSuggestions(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(count), "Count must be non-negative.");
        }

        return _suggestions.Values
            .OrderByDescending(s => s.TotalDuration)
            .Take(count)
            .Select(s => new AggregatedIndexSuggestion(
                s.Suggestion,
                s.Count,
                s.TotalDuration
            ));
    }

    /// <summary>
    /// Gets all aggregated index suggestions.
    /// </summary>
    public IEnumerable<AggregatedIndexSuggestion> AllSuggestions()
    {
        return _suggestions.Values
            .OrderByDescending(s => s.TotalDuration)
            .Select(s => new AggregatedIndexSuggestion(
                s.Suggestion,
                s.Count,
                s.TotalDuration
            ));
    }

    /// <summary>
    /// Gets the total number of aggregated suggestions.
    /// </summary>
    public int Count => _suggestions.Count;

    /// <summary>
    /// Clears all aggregated suggestions.
    /// </summary>
    public void Clear()
    {
        _suggestions.Clear();
    }

    /// <summary>
    /// Aggregated index suggestion with statistics.
    /// </summary>
    public sealed record AggregatedIndexSuggestion(IndexSuggestion Suggestion, int Count, TimeSpan TotalDuration)
    {
        /// <summary>
        /// The table name.
        /// </summary>
        public string Table => Suggestion.Table;

        /// <summary>
        /// The indexed columns.
        /// </summary>
        public IReadOnlyList<string> Columns => Suggestion.Columns;

        /// <summary>
        /// The reason for the index suggestion.
        /// </summary>
        public string Reason => Suggestion.Reason;

        /// <summary>
        /// The include columns for the index.
        /// </summary>
        public IReadOnlyList<string>? IncludeColumns => Suggestion.IncludeColumns;

        /// <summary>
        /// Gets the total duration attributed to this suggestion across all samples.
        /// </summary>
        public double TotalDurationMs => TotalDuration.TotalMilliseconds;

        /// <summary>
        /// Gets the average duration per occurrence of this suggestion.
        /// </summary>
        public double AverageDurationMs => Count > 0 ? TotalDurationMs / Count : 0;
    }

    /// <summary>
    /// Internal representation of an aggregated suggestion with mutable state.
    /// </summary>
    private sealed class AggregatedSuggestion
    {
        public IndexSuggestion Suggestion { get; set; } = null!;
        public int Count { get; set; }
        public TimeSpan TotalDuration { get; set; }
    }

    /// <summary>
    /// Key for comparing index suggestions by structural equality (Table, Columns, Reason).
    /// </summary>
    private sealed class IndexSuggestionKey : IEquatable<IndexSuggestionKey>
    {
        public string Table { get; }
        public string[] Columns { get; }
        public string Reason { get; }

        public IndexSuggestionKey(IndexSuggestion suggestion)
        {
            Table = suggestion.Table ?? string.Empty;
            Columns = suggestion.Columns?.ToArray() ?? Array.Empty<string>();
            Reason = suggestion.Reason ?? string.Empty;
        }

        public bool Equals(IndexSuggestionKey? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (!string.Equals(Table, other.Table, StringComparison.OrdinalIgnoreCase)) return false;
            if (Columns.Length != other.Columns.Length) return false;
            for (int i = 0; i < Columns.Length; i++)
            {
                if (!string.Equals(Columns[i], other.Columns[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            return string.Equals(Reason, other.Reason, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => Equals(obj as IndexSuggestionKey);

        public override int GetHashCode()
        {
            var hash = new HashCode();
            hash.Add(Table, StringComparer.OrdinalIgnoreCase);
            foreach (var col in Columns)
            {
                hash.Add(col, StringComparer.OrdinalIgnoreCase);
            }
            hash.Add(Reason, StringComparer.OrdinalIgnoreCase);
            return hash.ToHashCode();
        }
    }

    /// <summary>
    /// Comparer for IndexSuggestionKey that performs case-insensitive comparison.
    /// </summary>
    private sealed class IndexSuggestionKeyComparer : IEqualityComparer<IndexSuggestionKey>
    {
        public static readonly IndexSuggestionKeyComparer Instance = new();

        public bool Equals(IndexSuggestionKey? x, IndexSuggestionKey? y)
        {
            return x?.Equals(y) ?? y is null;
        }

        public int GetHashCode(IndexSuggestionKey obj)
        {
            return obj.GetHashCode();
        }
    }
}
