using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace EfCore.SlowQueryLog.Options;

/// <summary>
/// Selects which <see cref="Reporting.ISlowQueryRanking"/> implementation is used to rank captured
/// slow queries.
/// </summary>
public enum SlowQueryRankingMode
{
    /// <summary>
    /// Rank individual samples by exact SQL text, retaining the raw slowest samples
    /// (see <see cref="Reporting.SlowQueryRanking"/>). This is the default.
    /// </summary>
    Exact,

    /// <summary>
    /// Rank normalized SQL fingerprints (samples grouped by SQL text with aggregated statistics),
    /// see <see cref="Reporting.SlowQueryFingerprintRanking"/>.
    /// </summary>
    Fingerprint
}

/// <summary>
/// Configuration for the slow query interceptor.
/// </summary>
public sealed class SlowQueryLogOptions
{
    /// <summary>
    /// Commands whose execution takes at least this long are treated as slow.
    /// Defaults to 500ms.
    /// </summary>
    public TimeSpan Threshold { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Per‑provider threshold overrides. The key is the provider's connection type name
    /// (e.g. <c>SqlConnection</c>, <c>SqliteConnection</c>). If a provider name is present
    /// in this dictionary its value is used instead of <see cref="Threshold"/>.
    /// </summary>
    public IDictionary<string, TimeSpan> ProviderThresholds { get; set; } = new Dictionary<string, TimeSpan>();

    /// <summary>
    /// Log level used when a slow query is reported. Defaults to <see cref="LogLevel.Warning"/>.
    /// </summary>
    public LogLevel LogLevel { get; set; } = LogLevel.Warning;

    /// <summary>
    /// When true the parameter values are included in the logged SQL. Off by default to
    /// avoid leaking sensitive data into logs.
    /// </summary>
    public bool IncludeParameterValues { get; set; }

    /// <summary>
    /// When true the interceptor attempts to produce naive index suggestions by parsing
    /// the WHERE / JOIN / ORDER BY clauses of the offending SQL. Defaults to true.
    /// </summary>
    public bool SuggestIndexes { get; set; } = true;

    /// <summary>
    /// How many of the slowest queries to retain in the in‑memory ranking. Defaults to 25.
    /// </summary>
    public int RankingCapacity { get; set; } = 25;

    /// <summary>
    /// Selects which <see cref="Reporting.ISlowQueryRanking"/> implementation
    /// <see cref="Reporting.SlowQueryRankingFactory.Create"/> constructs for these options.
    /// Defaults to <see cref="SlowQueryRankingMode.Exact"/>.
    /// </summary>
    public SlowQueryRankingMode RankingMode { get; set; } = SlowQueryRankingMode.Exact;

    /// <summary>
    /// Optional sink invoked for every slow query in addition to the logger. Useful for
    /// pushing samples to a dashboard or metrics pipeline.
    /// </summary>
    public Action<SlowQuerySample>? OnSlowQuery { get; set; }

    /// <summary>
    /// When true (default) parameter values are redacted in captured samples. The
    /// parameter name and type are retained, but the value is replaced with '?'.
    /// When false the original behaviour (full value) is used.
    /// </summary>
    public bool RedactParameters { get; set; } = true;

    /// <summary>
    /// Sampling rate for slow queries (0.0 to 1.0). When set to a value less than 1.0,
    /// only a deterministic sample of slow queries will be recorded based on the fingerprint hash.
    /// This helps bound overhead when many slow queries occur. Defaults to 1.0 (all queries).
    /// </summary>
    public double SamplingRate { get; set; } = 1.0;

    /// <summary>
    /// Maximum number of slow query samples to retain in memory. When this limit is reached,
    /// the oldest samples are evicted using a ring-buffer strategy to prevent unbounded memory growth.
    /// Defaults to 1000 samples. This is separate from <see cref="RankingCapacity"/> which controls
    /// the size of the ranked results displayed in reports.
    /// </summary>
    public int MaxSamples { get; set; } = 1000;

    /// <summary>
    /// Maximum number of index suggestions to analyze per minute using a token-bucket algorithm.
    /// When this limit is reached, additional slow queries will not have index suggestions generated
    /// until the bucket refills. Set to 0 for unlimited analysis (not recommended for production).
    /// Defaults to 1000 suggestions per minute.
    /// </summary>
    public int MaxAnalysesPerMinute { get; set; } = 1000;

    /// <summary>
    /// When true, SQL text is queued for background analysis of index suggestions.
    /// The analysis runs off the hot path on a bounded channel, and samples are dropped
    /// when the channel is full. This prevents the interceptor from becoming a bottleneck
    /// during query execution. When false, analysis runs synchronously on the calling thread.
    /// Defaults to true.
    /// </summary>
    public bool AnalyzeOnBackgroundThread { get; set; } = true;

    /// <summary>
    /// Bounded channel capacity for background analysis queue. When the channel is full,
    /// new samples are dropped. This prevents unbounded memory growth during spikes in slow queries.
    /// Defaults to 1000 items.
    /// </summary>
    public int BackgroundQueueCapacity { get; set; } = 1000;

    internal void Validate()
    {
        if (Threshold <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Threshold), "Threshold must be positive.");

        if (RankingCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(RankingCapacity), "RankingCapacity must be positive.");

        if (MaxSamples <= 0)
            throw new ArgumentOutOfRangeException(nameof(MaxSamples), "MaxSamples must be positive.");

        if (MaxAnalysesPerMinute < 0)
            throw new ArgumentOutOfRangeException(nameof(MaxAnalysesPerMinute), "MaxAnalysesPerMinute must be non-negative.");

        if (BackgroundQueueCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(BackgroundQueueCapacity), "BackgroundQueueCapacity must be positive.");

        if (SamplingRate < 0.0 || SamplingRate > 1.0)
            throw new ArgumentOutOfRangeException(nameof(SamplingRate), "SamplingRate must be between 0.0 and 1.0.");

        if (ProviderThresholds == null)
            return; // nothing to validate

        foreach (var kvp in ProviderThresholds)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
                throw new ArgumentException("Provider name in ProviderThresholds cannot be null or whitespace.", nameof(ProviderThresholds));

            if (kvp.Value <= TimeSpan.Zero)
                throw new ArgumentOutOfRangeException(nameof(ProviderThresholds), $"Threshold for provider '{kvp.Key}' must be positive.");
        }
    }
}