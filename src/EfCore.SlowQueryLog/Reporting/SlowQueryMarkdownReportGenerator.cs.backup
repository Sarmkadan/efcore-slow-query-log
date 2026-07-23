using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Generates a Markdown report of slow query statistics and fingerprints.
/// </summary>
public static class SlowQueryMarkdownReportGenerator
{
    /// <summary>
    /// Generates a Markdown report from a SlowQueryRanking containing slow query samples.
    /// </summary>
    /// <param name="ranking">The ranking containing slow query samples.</param>
    /// <param name="topN">Number of top fingerprints to include in the report (default: 20).</param>
    /// <returns>A Markdown-formatted string with slow query statistics and top fingerprints.</returns>
    /// <exception cref="ArgumentNullException">Thrown if ranking is null.</exception>
    public static string GenerateReport(SlowQueryRanking ranking, int topN = 20)
    {
        ArgumentNullException.ThrowIfNull(ranking);

        var samples = ranking.Snapshot();
        var fingerprints = ranking.GetFingerprints();

        var report = new StringBuilder();

        // Header
        report.AppendLine("# Slow Query Analysis Report");
        report.AppendLine();
        report.AppendLine($"Generated: {DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss zzz}");
        report.AppendLine();

        // Summary statistics
        report.AppendLine("## Summary Statistics");
        report.AppendLine();
        report.AppendLine("| Metric | Value |");
        report.AppendLine("|--------|-------|");
        report.AppendLine($"| Total Queries | {samples.Count:N0} |");
        report.AppendLine($"| Top Fingerprints | {fingerprints.Count:N0} |");
        report.AppendLine($"| Total Duration | {ranking.GetTotalDuration().TotalMilliseconds:N0} ms |");
        report.AppendLine($"| Avg Duration | {ranking.GetAverageDuration():N2} ms |");
        report.AppendLine();

        // Top N fingerprints table
        report.AppendLine("## Top Slow Query Fingerprints");
        report.AppendLine();
        report.AppendLine("| Rank | Fingerprint | Count | Avg Duration | Max Duration | Total Duration | Sample SQL |");
        report.AppendLine("|------|-------------|-------|--------------|--------------|----------------|------------|");

        for (int i = 0; i < Math.Min(topN, fingerprints.Count); i++)
        {
            var fingerprint = fingerprints[i];
            var truncatedSql = TruncateSql(fingerprint.Sql, 60);

            report.AppendLine($"| {i + 1} | `{TruncateFingerprint(fingerprint.Sql)}` | {fingerprint.SampleCount:N0} " +
                           $"| {fingerprint.AverageDuration.TotalMilliseconds:N2} ms | " +
                           $"{fingerprint.MaxDuration.TotalMilliseconds:N2} ms | " +
                           $"{fingerprint.TotalDuration.TotalMilliseconds:N2} ms | " +
                           $"`{truncatedSql}` |")
                           .Replace("|", "&#124;")
                           .Replace("\n", " ")
                           .Replace("\r", " ");
        }

        if (fingerprints.Count > topN)
        {
            report.AppendLine($"| ... | ... | ... | ... | ... | ... | ... |");
        }

        report.AppendLine();

        // Index suggestions
        var allSuggestions = ranking.GetAllSuggestions().ToList();
        if (allSuggestions.Count > 0)
        {
            report.AppendLine("## Index Suggestions");
            report.AppendLine();
            report.AppendLine("| Table | Columns | Reason | Include Columns | SQL Hint |");
            report.AppendLine("|-------|---------|--------|----------------|----------|");

            foreach (var suggestion in allSuggestions.Distinct())
            {
                var sqlHint = suggestion.ToSqlHint();
                var truncatedHint = TruncateSql(sqlHint, 80);

                report.AppendLine($"| `{suggestion.Table}` | `{string.Join(", ", suggestion.Columns)}` | " +
                               $"`{suggestion.Reason}` | " +
                               $"`{string.Join(", ", suggestion.IncludeColumns ?? Array.Empty<string>())}` | " +
                               $"`{truncatedHint}` |");
            }

            report.AppendLine();
        }

        // Details for top fingerprints
        if (fingerprints.Count > 0)
        {
            report.AppendLine("## Top Fingerprint Details");
            report.AppendLine();

            for (int i = 0; i < Math.Min(5, fingerprints.Count); i++)
            {
                var fingerprint = fingerprints[i];
                report.AppendLine($"### Fingerprint #{i + 1}");
                report.AppendLine();
                report.AppendLine($"**SQL:** `{TruncateSql(fingerprint.Sql, 120)}`");
                report.AppendLine();
                report.AppendLine($"- Count: {fingerprint.SampleCount}");
                report.AppendLine($"- Avg Duration: {fingerprint.AverageDuration.TotalMilliseconds:N2} ms");
                report.AppendLine($"- Max Duration: {fingerprint.MaxDuration.TotalMilliseconds:N2} ms");
                report.AppendLine($"- Min Duration: {fingerprint.MinDuration.TotalMilliseconds:N2} ms");
                report.AppendLine($"- Total Duration: {fingerprint.TotalDuration.TotalMilliseconds:N2} ms");
                report.AppendLine($"- P50 Duration: {fingerprint.Percentile50.TotalMilliseconds:N2} ms");
                report.AppendLine($"- P95 Duration: {fingerprint.Percentile95.TotalMilliseconds:N2} ms");
                report.AppendLine($"- P99 Duration: {fingerprint.Percentile99.TotalMilliseconds:N2} ms");

                if (fingerprint.Parameters != null)
                {
                    report.AppendLine($"- Parameters: `{fingerprint.Parameters}`");
                }

                if (fingerprint.Suggestions.Count > 0)
                {
                    report.AppendLine("- **Index Suggestions:**");
                    foreach (var suggestion in fingerprint.Suggestions)
                    {
                        report.AppendLine($"  - {suggestion.ToSqlHint()}");
                    }
                }

                report.AppendLine();
            }
        }

        return report.ToString();
    }

    /// <summary>
    /// Generates a Markdown report file from a SlowQueryRanking.
    /// </summary>
    /// <param name="filePath">The path to the output Markdown file.</param>
    /// <param name="ranking">The ranking containing slow query samples.</param>
    /// <param name="topN">Number of top fingerprints to include in the report (default: 20).</param>
    /// <exception cref="ArgumentNullException">Thrown if filePath or ranking is null.</exception>
    /// <exception cref="ArgumentException">Thrown if filePath is empty or whitespace.</exception>
    public static void WriteReport(string filePath, SlowQueryRanking ranking, int topN = 20)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        ArgumentNullException.ThrowIfNull(ranking);

        var report = GenerateReport(ranking, topN);
        System.IO.File.WriteAllText(filePath, report);
    }

    private static string TruncateSql(string sql, int maxLength)
    {
        if (string.IsNullOrEmpty(sql))
        {
            return sql ?? string.Empty;
        }

        if (sql.Length <= maxLength)
        {
            return sql;
        }

        // Truncate and add ellipsis
        return sql.Substring(0, maxLength - 3) + "...";
    }

    private static string TruncateFingerprint(string sql)
    {
        // Create a short fingerprint identifier from the SQL
        if (string.IsNullOrEmpty(sql))
        {
            return "n/a";
        }

        // Extract first meaningful part of the query
        var trimmed = sql.Trim();
        if (trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
        {
            var fromIndex = trimmed.IndexOf("FROM", StringComparison.OrdinalIgnoreCase);
            if (fromIndex > 0)
            {
                var fingerprint = trimmed.Substring(0, Math.Min(40, fromIndex)).Trim();
                return fingerprint + (fingerprint.Length < fromIndex ? "..." : "");
            }
        }
        else if (trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) ||
                 trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) ||
                 trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase))
        {
            var spaceIndex = trimmed.IndexOf(' ');
            if (spaceIndex > 0)
            {
                var fingerprint = trimmed.Substring(0, Math.Min(40, spaceIndex + 10)).Trim();
                return fingerprint + (fingerprint.Length < trimmed.Length ? "..." : "");
            }
        }

        return trimmed.Length <= 40 ? trimmed : trimmed.Substring(0, 40) + "...";
    }
}
