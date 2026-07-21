using System;
using System.Buffers;
using System.Data.Common;
using System.Text;
using EfCore.SlowQueryLog.Analysis;
using EfCore.SlowQueryLog.Options;
using EfCore.SlowQueryLog.Reporting;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EfCore.SlowQueryLog.Interception;

/// <summary>
/// An <see cref="IDbCommandInterceptor"/> that measures every executed command and,
/// when it exceeds the configured threshold, logs the generated SQL together with a
/// ranked summary and naive index suggestions.
/// </summary>
public sealed class SlowQueryInterceptor : DbCommandInterceptor
{
    private readonly SlowQueryLogOptions _options;
    private readonly ILogger<SlowQueryInterceptor> _logger;
    private readonly IndexSuggestionAnalyzer _analyzer;

    public SlowQueryInterceptor(
        SlowQueryLogOptions options,
        ILogger<SlowQueryInterceptor>? logger = null,
        SlowQueryRanking? ranking = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SlowQueryInterceptor>.Instance;
        _analyzer = new IndexSuggestionAnalyzer();
        Ranking = ranking ?? new SlowQueryRanking(_options.RankingCapacity);
    }

    /// <summary>The live ranking of slow queries, exposed for reporting / dashboards.</summary>
    public SlowQueryRanking Ranking { get; }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        Capture(command, eventData.Duration);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        Capture(command, eventData.Duration);
        return await base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        Capture(command, eventData.Duration);
        return base.NonQueryExecuted(command, eventData, result);
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        Capture(command, eventData.Duration);
        return await base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        Capture(command, eventData.Duration);
        return base.ScalarExecuted(command, eventData, result);
    }

    public override async ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        Capture(command, eventData.Duration);
        return await base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
    }

    /// <summary>
    /// Core capture path. Public so it can be exercised directly from tests without a
    /// live database connection.
    /// </summary>
    public SlowQuerySample? Capture(DbCommand command, TimeSpan duration)
    {
        // Determine the effective threshold, taking per‑provider overrides into account.
        var effectiveThreshold = GetEffectiveThreshold(command);

        if (duration < effectiveThreshold)
            return null;

        var suggestions = _options.SuggestIndexes
            ? _analyzer.Analyze(command.CommandText)
            : Array.Empty<IndexSuggestion>();

        var sample = new SlowQuerySample
        {
            Sql = command.CommandText,
            Duration = duration,
            CapturedAt = DateTimeOffset.UtcNow,
            Parameters = _options.IncludeParameterValues ? FormatParameters(command, _options.RedactParameters) : null,
            Suggestions = suggestions,
        };

        Ranking.Add(sample);
        Report(sample);
        try
        {
            _options.OnSlowQuery?.Invoke(sample);
        }
        catch
        {
            // Exceptions in the callback must not break query execution
        }

        return sample;
    }

    private TimeSpan GetEffectiveThreshold(DbCommand command)
    {
        // Use the connection type name as the provider identifier.
        var providerName = command?.Connection?.GetType().Name ?? string.Empty;

        if (_options.ProviderThresholds != null &&
            _options.ProviderThresholds.TryGetValue(providerName, out var providerThreshold))
        {
            return providerThreshold;
        }

        return _options.Threshold;
    }

    private void Report(SlowQuerySample sample)
    {
        if (!_logger.IsEnabled(_options.LogLevel))
            return;

        var sb = new StringBuilder();
        sb.Append("Slow query detected: ")
          .Append(sample.Duration.TotalMilliseconds.ToString("F1"))
          .AppendLine("ms");
        sb.AppendLine(sample.Sql.Trim());

        if (sample.Parameters is not null)
            sb.Append("Parameters: ").AppendLine(sample.Parameters);

        if (sample.Suggestions.Count > 0)
        {
            sb.AppendLine("Index suggestions:");
            foreach (var s in sample.Suggestions)
                sb.Append("  ").AppendLine(s.ToSqlHint());
        }

        _logger.Log(_options.LogLevel, "{SlowQueryReport}", sb.ToString());
    }

    private static string FormatParameters(DbCommand command, bool redact)
    {
        // Use optimized version to minimize allocations
        return FormatParametersOptimized(command, redact);
    }

    /// <summary>
    /// Optimized parameter formatting that minimizes allocations by using ArrayPool and avoiding intermediate strings.
    /// </summary>
    private static string FormatParametersOptimized(DbCommand command, bool redact)
    {
        if (command.Parameters.Count == 0)
            return "(none)";

        // Use ArrayPool to avoid allocations for the parts list
        var rentedArray = ArrayPool<string>.Shared.Rent(command.Parameters.Count);
        var count = 0;
        var totalLength = 0;

        try
        {
            foreach (DbParameter p in command.Parameters)
            {
                if (redact)
                {
                    // Keep name and type, replace value with '?'
                    var typeInfo = p.DbType.ToString();
                    var part = $"{p.ParameterName} ({typeInfo})=?";
                    rentedArray[count] = part;
                    totalLength += part.Length;
                }
                else
                {
                    var valueStr = p.Value?.ToString() ?? "NULL";
                    var part = $"{p.ParameterName}={valueStr}";
                    rentedArray[count] = part;
                    totalLength += part.Length;
                }
                count++;
            }

            // Allocate exactly what we need
            if (count == 0)
                return "(none)";

            var result = new StringBuilder(totalLength + (count - 1) * 2); // +2 for ", " separator
            result.Append(rentedArray[0]);

            for (var i = 1; i < count; i++)
            {
                result.Append(", ");
                result.Append(rentedArray[i]);
            }

            return result.ToString();
        }
        finally
        {
            ArrayPool<string>.Shared.Return(rentedArray);
        }
    }
}
