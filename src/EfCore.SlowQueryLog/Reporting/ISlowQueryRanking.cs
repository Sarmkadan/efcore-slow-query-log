using System;
using System.Collections.Generic;
using EfCore.SlowQueryLog.Analysis;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Common abstraction implemented by the different slow-query ranking strategies
/// (<see cref="SlowQueryRanking"/> for exact per-SQL ranking and
/// <see cref="SlowQueryFingerprintRanking"/> for normalized/parameter-stripped ranking),
/// so that reporting code (Markdown reports, aggregate statistics) can operate on either
/// strategy without depending on a concrete implementation.
/// </summary>
public interface ISlowQueryRanking
{
    /// <summary>
    /// Records a captured slow query sample into the ranking.
    /// </summary>
    /// <param name="sample">The slow query sample to record.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sample"/> is null.</exception>
    void Record(SlowQuerySample sample);

    /// <summary>
    /// Returns the top <paramref name="count"/> ranked fingerprints, ordered from highest ranked to lowest.
    /// </summary>
    /// <param name="count">The maximum number of fingerprints to return.</param>
    /// <returns>A list of at most <paramref name="count"/> fingerprints.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="count"/> is negative.</exception>
    IReadOnlyList<SlowQueryFingerprint> TopN(int count);

    /// <summary>
    /// Removes all recorded data from the ranking.
    /// </summary>
    void Clear();

    /// <summary>
    /// Gets the number of entries currently held by the ranking.
    /// </summary>
    int Count { get; }

    /// <summary>
    /// Gets the total duration attributed to all queries tracked by this ranking.
    /// </summary>
    TimeSpan TotalDuration { get; }

    /// <summary>
    /// Gets the average query duration, in milliseconds, across all queries tracked by this ranking.
    /// Returns 0.0 when the ranking is empty.
    /// </summary>
    double AverageDurationMs { get; }

    /// <summary>
    /// Gets all aggregated index suggestions collected by this ranking, deduplicated and
    /// ranked by total attributed duration.
    /// </summary>
    /// <returns>An enumerable of aggregated index suggestions with statistics.</returns>
    IEnumerable<IndexSuggestionAggregator.AggregatedIndexSuggestion> GetAllSuggestions();
}
