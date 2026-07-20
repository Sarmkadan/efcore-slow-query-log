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

    /// <summary>
    /// Groups samples by SQL fingerprint and computes aggregated statistics (P95, max duration, etc.).
    /// </summary>
    /// <param name="ranking">The <see cref="SlowQueryRanking"/> instance.</param>
    /// <returns>A list of fingerprints with aggregated statistics, ordered by average duration descending.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IReadOnlyList<SlowQueryFingerprint> GetFingerprints(this SlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        return ranking.GetFingerprints();
    }

    /// <summary>
    /// Gets fingerprints ordered by total cumulative duration (TotalTimeRank).
    /// </summary>
    /// <param name="ranking">The <see cref="SlowQueryRanking"/> instance.</param>
    /// <returns>A list of fingerprints ordered by total duration descending.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IReadOnlyList<SlowQueryFingerprint> GetFingerprintsByTotalDuration(this SlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        var fingerprints = ranking.GetFingerprints();
        return fingerprints.OrderByDescending(f => f.TotalDuration).ToList();
    }

    /// <summary>
    /// Gets fingerprints ordered by P95 duration.
    /// </summary>
    /// <param name="ranking">The <see cref="SlowQueryRanking"/> instance.</param>
    /// <returns>A list of fingerprints ordered by P95 duration descending.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IReadOnlyList<SlowQueryFingerprint> GetFingerprintsByP95Duration(this SlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        var fingerprints = ranking.GetFingerprints();
        return fingerprints.OrderByDescending(f => f.Percentile95).ToList();
    }

    /// <summary>
    /// Gets fingerprints ordered by max duration.
    /// </summary>
    /// <param name="ranking">The <see cref="SlowQueryRanking"/> instance.</param>
    /// <returns>A list of fingerprints ordered by max duration descending.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IReadOnlyList<SlowQueryFingerprint> GetFingerprintsByMaxDuration(this SlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        var fingerprints = ranking.GetFingerprints();
        return fingerprints.OrderByDescending(f => f.MaxDuration).ToList();
    }

    /// <summary>
    /// Exports the current ranking (samples and fingerprint aggregates) to a JSON file.
    /// </summary>
    /// <param name="ranking">The <see cref="SlowQueryRanking"/> instance.</param>
    /// <param name="filePath">The path of the file to write the JSON report to.</param>
    /// <param name="indented">If <c>true</c>, the JSON will be formatted with indentation.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> or <paramref name="filePath"/> is null.</exception>
    public static void ExportToJson(this SlowQueryRanking ranking, string filePath, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        ArgumentNullException.ThrowIfNull(filePath);
        SlowQueryReportWriter.WriteReport(filePath, ranking, indented);
    }
}
