using System.Text.Json;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Provides System.Text.Json serialization and deserialization helpers for <see cref="ISlowQueryRanking"/>.
/// </summary>
public static class SlowQueryRankingJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    /// <summary>
    /// Serializes a <see cref="ISlowQueryRanking"/> instance to a JSON report string.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the ranking report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJsonReport(this ISlowQueryRanking value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var report = new ReportDto
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Samples = GetSamples(value),
            Fingerprints = GetFingerprints(value)
        };

        var options = indented
            ? new JsonSerializerOptions(_options) { WriteIndented = true }
            : _options;

        return JsonSerializer.Serialize(report, options);
    }

    private static IReadOnlyList<SlowQuerySample> GetSamples(ISlowQueryRanking ranking)
    {
        // SlowQueryFingerprintRanking doesn't retain individual samples, so return empty list
        if (ranking is SlowQueryRanking exactRanking)
        {
            return exactRanking.Snapshot();
        }

        return Array.Empty<SlowQuerySample>();
    }

    private static IReadOnlyList<SlowQueryFingerprint> GetFingerprints(ISlowQueryRanking ranking)
    {
        // SlowQueryFingerprintRanking already has fingerprints directly
        if (ranking is SlowQueryFingerprintRanking fingerprintRanking)
        {
            return fingerprintRanking.Snapshot();
        }

        // For SlowQueryRanking, compute fingerprints from samples
        if (ranking is SlowQueryRanking exactRanking)
        {
            return exactRanking.GetFingerprints();
        }

        return Array.Empty<SlowQueryFingerprint>();
    }

    /// <summary>
    /// Serializes a list of fingerprints to a JSON string.
    /// </summary>
    /// <param name="fingerprints">The fingerprints to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the fingerprints.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="fingerprints"/> is null.</exception>
    public static string ToJson(this IReadOnlyList<SlowQueryFingerprint> fingerprints, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(fingerprints);

        var options = indented
            ? new JsonSerializerOptions(_options) { WriteIndented = true }
            : _options;

        return JsonSerializer.Serialize(fingerprints, options);
    }

    private sealed class ReportDto
    {
        public DateTimeOffset GeneratedAt { get; set; }
        public IReadOnlyList<SlowQuerySample> Samples { get; set; } = Array.Empty<SlowQuerySample>();
        public IReadOnlyList<SlowQueryFingerprint> Fingerprints { get; set; } = Array.Empty<SlowQueryFingerprint>();
    }
}