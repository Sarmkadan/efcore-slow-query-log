namespace EfCore.SlowQueryLog;

/// <summary>
/// Extension methods for <see cref="SlowQuerySample"/> to provide common query analysis operations.
/// </summary>
public static class SlowQuerySampleExtensions
{
    /// <summary>
    /// Determines whether the query duration is slower than the specified threshold.
    /// </summary>
    /// <param name="sample">The slow query sample to check.</param>
    /// <param name="threshold">The duration threshold to compare against.</param>
    /// <returns>True if the query duration is greater than the threshold; otherwise, false.</returns>
    public static bool IsSlowerThan(this SlowQuerySample sample, TimeSpan threshold)
    {
        ArgumentNullException.ThrowIfNull(sample);
        return sample.Duration > threshold;
    }

    /// <summary>
    /// Gets a normalized version of the SQL query for grouping and comparison purposes.
    /// Normalization includes:
    /// - Collapsing consecutive whitespace characters into single spaces
    /// - Converting the SQL to lowercase
    /// - Removing leading/trailing whitespace
    /// </summary>
    /// <param name="sample">The slow query sample.</param>
    /// <returns>A normalized SQL string suitable for grouping similar queries.</returns>
    public static string GetNormalizedSql(this SlowQuerySample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        if (string.IsNullOrWhiteSpace(sample.Sql))
        {
            return string.Empty;
        }

        // Collapse consecutive whitespace into single spaces
        var normalized = System.Text.RegularExpressions.Regex.Replace(sample.Sql, "\\s+", " ");

        // Convert to lowercase
        normalized = normalized.ToLowerInvariant();

        // Trim leading/trailing whitespace
        normalized = normalized.Trim();

        return normalized;
    }

    /// <summary>
    /// Creates a single-line log string representation of the slow query sample.
    /// Format: "[duration] sql..." where SQL is truncated to 120 characters.
    /// </summary>
    /// <param name="sample">The slow query sample.</param>
    /// <returns>A single-line summary string for logging purposes.</returns>
    public static string ToLogString(this SlowQuerySample sample)
    {
        ArgumentNullException.ThrowIfNull(sample);

        // Format duration as total milliseconds
        var durationMs = sample.Duration.TotalMilliseconds;

        // Truncate SQL to 120 characters
        var sql = sample.Sql;
        if (sql.Length > 120)
        {
            sql = sql[..120] + "...";
        }

        return $"[{durationMs:F2}ms] {sql}";
    }
}