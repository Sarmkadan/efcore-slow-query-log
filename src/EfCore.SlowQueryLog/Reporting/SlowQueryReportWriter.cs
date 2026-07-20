using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Writes a JSON report containing the collected slow‑query samples and their aggregated fingerprint data.
/// </summary>
public static class SlowQueryReportWriter
{
    /// <summary>
    /// Writes a JSON file that contains the current samples and fingerprint aggregates from the provided ranking.
    /// </summary>
    /// <param name="filePath">The destination file path.</param>
    /// <param name="ranking">The <see cref="SlowQueryRanking"/> whose data should be exported.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="filePath"/> or <paramref name="ranking"/> is null.</exception>
    /// <exception cref="IOException">Thrown if the file cannot be written.</exception>
    public static void WriteReport(string filePath, SlowQueryRanking ranking, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(filePath);
        ArgumentNullException.ThrowIfNull(ranking);

        var report = new ReportDto
        {
            GeneratedAt = DateTimeOffset.UtcNow,
            Samples = ranking.Snapshot(),
            Fingerprints = ranking.GetFingerprints()
        };

        var options = new JsonSerializerOptions
        {
            WriteIndented = indented,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        string json = JsonSerializer.Serialize(report, options);
        File.WriteAllText(filePath, json);
    }

    private sealed class ReportDto
    {
        public DateTimeOffset GeneratedAt { get; set; }

        public IReadOnlyList<SlowQuerySample> Samples { get; set; } = Array.Empty<SlowQuerySample>();

        public IReadOnlyList<SlowQueryFingerprint> Fingerprints { get; set; } = Array.Empty<SlowQueryFingerprint>();
    }
}
