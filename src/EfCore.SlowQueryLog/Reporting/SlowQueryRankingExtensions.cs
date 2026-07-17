using System;
using System.Collections.Generic;
using System.Linq;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Provides extension methods for <see cref="SlowQueryRanking"/>.
/// </summary>
public static class SlowQueryRankingExtensions
{
    /// <summary>
    /// Calculates the total duration of all slow queries in the ranking.
    /// </summary>
    /// <param name="ranking">The <see cref="SlowQueryRanking"/> instance.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the total duration.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static TimeSpan GetTotalDuration(this SlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        var snapshot = ranking.Snapshot();
        return snapshot.Count == 0
            ? TimeSpan.Zero
            : TimeSpan.FromMilliseconds(snapshot.Sum(static s => s.Duration.TotalMilliseconds));
    }

    /// <summary>
    /// Calculates the average duration of the slow queries in the ranking.
    /// Returns 0.0 if there are no queries.
    /// </summary>
    /// <param name="ranking">The <see cref="SlowQueryRanking"/> instance.</param>
    /// <returns>A double representing the average duration in milliseconds.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static double GetAverageDuration(this SlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        var snapshot = ranking.Snapshot();
        return snapshot.Count == 0
            ? 0.0
            : snapshot.Average(static s => s.Duration.TotalMilliseconds);
    }

    /// <summary>
    /// Returns all index suggestions from all queries in the ranking.
    /// </summary>
    /// <param name="ranking">The <see cref="SlowQueryRanking"/> instance.</param>
    /// <returns>An <see cref="IEnumerable{IndexSuggestion}"/> containing all suggestions.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IEnumerable<IndexSuggestion> GetAllSuggestions(this SlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        return ranking.Snapshot().SelectMany(static s => s.Suggestions);
    }
}
