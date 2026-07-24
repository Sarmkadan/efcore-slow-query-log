using System;
using System.Collections.Generic;
using System.Linq;
using EfCore.SlowQueryLog.Analysis;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Provides extension methods for <see cref="SlowQueryRanking"/>, <see cref="SlowQueryFingerprintRanking"/>, and <see cref="ISlowQueryRanking"/>.
/// </summary>
public static class SlowQueryRankingExtensions
{
    /// <summary>
    /// Calculates the total duration of all slow queries in the ranking.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the total duration.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static TimeSpan GetTotalDuration(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        return ranking.TotalDuration;
    }

    /// <summary>
    /// Calculates the average duration of the slow queries in the ranking.
    /// Returns 0.0 if there are no queries.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>A double representing the average duration in milliseconds.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static double GetAverageDuration(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        return ranking.AverageDurationMs;
    }

    /// <summary>
    /// Returns all aggregated index suggestions from all queries in the ranking.
    /// Suggestions are deduplicated and ranked by total attributed duration.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>An <see cref="IEnumerable{AggregatedIndexSuggestion}"/> containing aggregated suggestions with statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IEnumerable<IndexSuggestionAggregator.AggregatedIndexSuggestion> GetAllSuggestions(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        return ranking.GetAllSuggestions();
    }

    /// <summary>
    /// Groups samples by SQL fingerprint and computes aggregated statistics (P95, max duration, etc.).
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>A list of fingerprints with aggregated statistics, ordered by average duration descending.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IReadOnlyList<SlowQueryFingerprint> GetFingerprints(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);

        return ranking switch
        {
            SlowQueryRanking exactRanking => exactRanking.GetFingerprints(),
            SlowQueryFingerprintRanking fingerprintRanking => fingerprintRanking.Snapshot(),
            _ => Array.Empty<SlowQueryFingerprint>()
        };
    }

    /// <summary>
    /// Gets fingerprints ordered by total cumulative duration (TotalTimeRank).
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>A list of fingerprints ordered by total duration descending.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IReadOnlyList<SlowQueryFingerprint> GetFingerprintsByTotalDuration(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        var fingerprints = ranking.GetFingerprints();
        return fingerprints.OrderByDescending(f => f.TotalDuration).ToList();
    }

    /// <summary>
    /// Gets fingerprints ordered by P95 duration.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>A list of fingerprints ordered by P95 duration descending.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IReadOnlyList<SlowQueryFingerprint> GetFingerprintsByP95Duration(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        var fingerprints = ranking.GetFingerprints();
        return fingerprints.OrderByDescending(f => f.Percentile95).ToList();
    }

    /// <summary>
    /// Gets fingerprints ordered by max duration.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>A list of fingerprints ordered by max duration descending.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IReadOnlyList<SlowQueryFingerprint> GetFingerprintsByMaxDuration(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        var fingerprints = ranking.GetFingerprints();
        return fingerprints.OrderByDescending(f => f.MaxDuration).ToList();
    }

    /// <summary>
    /// Exports the current ranking (samples and fingerprint aggregates) to a JSON file.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <param name="filePath">The path of the file to write the JSON report to.</param>
    /// <param name="indented">If <c>true</c>, the JSON will be formatted with indentation.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> or <paramref name="filePath"/> is null.</exception>
    public static void ExportToJson(this ISlowQueryRanking ranking, string filePath, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        ArgumentNullException.ThrowIfNull(filePath);
        SlowQueryReportWriter.WriteReport(filePath, ranking, indented);
    }

    /// <summary>
    /// Generates a Markdown-formatted report of the slow queries in this ranking.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <param name="topN">Number of top fingerprints to include (default: 20).</param>
    /// <returns>A Markdown-formatted string with slow query statistics and top fingerprints.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static string GenerateMarkdownReport(this ISlowQueryRanking ranking, int topN = 20)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        return SlowQueryMarkdownReportGenerator.GenerateReport(ranking, topN);
    }

    /// <summary>
    /// Writes a Markdown report file from this ranking.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <param name="filePath">The path to the output Markdown file.</param>
    /// <param name="topN">Number of top fingerprints to include (default: 20).</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> or <paramref name="filePath"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="filePath"/> is empty or whitespace.</exception>
    public static void WriteMarkdownReport(this ISlowQueryRanking ranking, string filePath, int topN = 20)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        SlowQueryMarkdownReportGenerator.WriteReport(filePath, ranking, topN);
    }

    /// <summary>
    /// Clears all captured slow queries from the ranking.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>The <see cref="ISlowQueryRanking"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static ISlowQueryRanking Clear(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        ranking.Clear();
        return ranking;
    }

    /// <summary>
    /// Gets the number of slow queries currently captured in the ranking.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>The count of captured slow queries.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static int GetCount(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        return ranking.Count;
    }

    /// <summary>
    /// Determines whether any slow queries have been captured in the ranking.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>True if at least one slow query has been captured; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static bool HasQueries(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);
        return ranking.Count > 0;
    }

    /// <summary>
    /// Gets the current snapshot of slow query samples from the ranking.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>A read-only list of slow query samples.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IReadOnlyList<SlowQuerySample> GetSamples(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);

        return ranking switch
        {
            SlowQueryRanking exactRanking => exactRanking.Snapshot(),
            _ => Array.Empty<SlowQuerySample>()
        };
    }

    /// <summary>
    /// Gets the current snapshot of fingerprints from the ranking.
    /// </summary>
    /// <param name="ranking">The <see cref="ISlowQueryRanking"/> instance.</param>
    /// <returns>A read-only list of fingerprints.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="ranking"/> is null.</exception>
    public static IReadOnlyList<SlowQueryFingerprint> GetFingerprintsSnapshot(this ISlowQueryRanking ranking)
    {
        ArgumentNullException.ThrowIfNull(ranking);

        return ranking switch
        {
            SlowQueryFingerprintRanking fingerprintRanking => fingerprintRanking.Snapshot(),
            SlowQueryRanking exactRanking => exactRanking.GetFingerprints(),
            _ => Array.Empty<SlowQueryFingerprint>()
        };
    }
}