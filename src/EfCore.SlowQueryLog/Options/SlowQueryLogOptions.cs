using Microsoft.Extensions.Logging;

namespace EfCore.SlowQueryLog.Options;

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
    /// How many of the slowest queries to retain in the in-memory ranking. Defaults to 25.
    /// </summary>
    public int RankingCapacity { get; set; } = 25;

    /// <summary>
    /// Optional sink invoked for every slow query in addition to the logger. Useful for
    /// pushing samples to a dashboard or metrics pipeline.
    /// </summary>
    public Action<SlowQuerySample>? OnSlowQuery { get; set; }

    internal void Validate()
    {
        if (Threshold <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(Threshold), "Threshold must be positive.");
        if (RankingCapacity <= 0)
            throw new ArgumentOutOfRangeException(nameof(RankingCapacity), "RankingCapacity must be positive.");
    }
}
