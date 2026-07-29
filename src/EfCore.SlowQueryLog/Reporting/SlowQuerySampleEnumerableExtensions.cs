using System;
using System.Collections.Generic;
using System.Linq;
using EfCore.SlowQueryLog;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Extension methods for <see cref="IEnumerable{SlowQuerySample}"/> providing aggregate
/// and CSV conversion utilities.
/// </summary>
public static class SlowQuerySampleEnumerableExtensions
{
    /// <summary>
    /// Calculates the total duration of all supplied samples.
    /// </summary>
    /// <param name="samples">The collection of <see cref="SlowQuerySample"/> instances.</param>
    /// <returns>The sum of the <c>Duration</c> property of each sample.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="samples"/> is <c>null</c>.</exception>
    public static TimeSpan TotalDuration(this IEnumerable<SlowQuerySample> samples)
    {
        if (samples == null) throw new ArgumentNullException(nameof(samples));

        // Aggregate the durations; using TimeSpan addition which is safe for overflow in normal usage.
        return samples.Aggregate(TimeSpan.Zero, (total, sample) => total + sample.Duration);
    }

    /// <summary>
    /// Returns the slowest sample that matches the supplied fingerprint.
    /// </summary>
    /// <param name="samples">The collection of <see cref="SlowQuerySample"/> instances.</param>
    /// <param name="fingerprint">
    /// The normalized SQL fingerprint to match. Matching is performed using the
    /// <see cref="SlowQuerySampleExtensions.GetNormalizedSql(SlowQuerySample)"/> extension method.
    /// </param>
    /// <returns>
    /// The sample with the greatest <c>Duration</c> for the given fingerprint, or <c>null</c>
    /// if no matching sample exists.
    /// </returns>
    /// <exception cref="ArgumentNullException">If <paramref name="samples"/> or <paramref name="fingerprint"/> is <c>null</c>.</exception>
    public static SlowQuerySample? SlowestBy(this IEnumerable<SlowQuerySample> samples, string fingerprint)
    {
        if (samples == null) throw new ArgumentNullException(nameof(samples));
        if (fingerprint == null) throw new ArgumentNullException(nameof(fingerprint));

        // Use the existing GetNormalizedSql extension to obtain the fingerprint for each sample.
        return samples
            .Where(s => string.Equals(s.GetNormalizedSql(), fingerprint, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(s => s.Duration)
            .FirstOrDefault();
    }

    /// <summary>
    /// Converts a collection of samples to CSV lines.
    /// </summary>
    /// <param name="samples">The collection of <see cref="SlowQuerySample"/> instances.</param>
    /// <returns>
    /// An <see cref="IEnumerable{String}"/> where the first line is a header
    /// (<c>DurationMs,Sql</c>) and each subsequent line represents a sample.
    /// </returns>
    /// <exception cref="ArgumentNullException">If <paramref name="samples"/> is <c>null</c>.</exception>
    public static IEnumerable<string> ToCsvLines(this IEnumerable<SlowQuerySample> samples)
    {
        if (samples == null) throw new ArgumentNullException(nameof(samples));

        // Header line
        yield return "DurationMs,Sql";

        foreach (var sample in samples)
        {
            // Escape double quotes in the SQL text for CSV compliance.
            var sql = sample.Sql?.Replace("\"", "\"\"");
            var sqlEscaped = sql != null ? $"\"{sql}\"" : string.Empty;

            // Duration expressed in milliseconds with invariant formatting.
            var durationMs = sample.Duration.TotalMilliseconds.ToString(System.Globalization.CultureInfo.InvariantCulture);

            yield return $"{durationMs},{sqlEscaped}";
        }
    }
}
