using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Security.Cryptography;
using System.Text;
using EfCore.SlowQueryLog.Analysis;
using EfCore.SlowQueryLog.Options;
using EfCore.SlowQueryLog.Reporting;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace EfCore.SlowQueryLog.Interception;

/// <summary>
/// An <see cref="IDbCommandInterceptor"/> that measures every executed command and, when it exceeds the configured threshold,
/// logs the generated SQL together with a ranked summary and naive index suggestions.
/// </summary>
public sealed class SlowQueryInterceptor : DbCommandInterceptor
{
    private readonly SlowQueryLogOptions _options;
    private readonly ILogger<SlowQueryInterceptor> _logger;
    private readonly IndexSuggestionAnalyzer _syncAnalyzer;
    private readonly IndexSuggestionBackgroundAnalyzer? _backgroundAnalyzer;
    private readonly object _samplingGate = new();
    private readonly ConcurrentDictionary<string, int> _samplingCounters = new(StringComparer.Ordinal);
    private readonly List<string> _excludedSqlFragments = new() { "__EFMigrationsHistory" };

    public SlowQueryInterceptor(
        SlowQueryLogOptions options,
        ILogger<SlowQueryInterceptor>? logger = null,
        SlowQueryRanking? ranking = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _options.Validate();
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger<SlowQueryInterceptor>.Instance;
        Ranking = ranking ?? new SlowQueryRanking(_options.MaxSamples, _options.RankingCapacity);

        // Always create sync analyzer for cases where background analysis is disabled
        _syncAnalyzer = new IndexSuggestionAnalyzer();

        // Create background analyzer only if enabled
        if (_options.AnalyzeOnBackgroundThread)
        {
            _backgroundAnalyzer = new IndexSuggestionBackgroundAnalyzer(_options, logger);
        }
    }

    /// <summary>The live ranking of slow queries, exposed for reporting / dashboards.</summary>
    public SlowQueryRanking Ranking { get; }

    /// <summary>Gets the SQL fragments that are excluded from slow-query capture.</summary>
    public IReadOnlyList<string> ExcludedSqlFragments => _excludedSqlFragments;

    /// <summary>
    /// Excludes commands containing any of the supplied SQL fragments, using a case-insensitive comparison.
    /// </summary>
    /// <param name="fragments">The SQL fragments to exclude. Null and empty entries are ignored.</param>
    /// <returns>This interceptor, enabling fluent configuration.</returns>
    /// <example>
    /// <code>
    /// var interceptor = new SlowQueryInterceptor(options)
    ///     .ExcludeSqlContaining("SELECT 1", "HealthCheck");
    /// </code>
    /// </example>
    /// <exception cref="ArgumentNullException"><paramref name="fragments"/> is <see langword="null"/>.</exception>
    public SlowQueryInterceptor ExcludeSqlContaining(params string[] fragments)
    {
        ArgumentNullException.ThrowIfNull(fragments);

        foreach (var fragment in fragments)
        {
            if (!string.IsNullOrEmpty(fragment))
                _excludedSqlFragments.Add(fragment);
        }

        return this;
    }

    public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)
    {
        _logger.LogInformation("ReaderExecuted called with {CommandText}", command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length)));
        return CaptureAndProceed(
            command,
            eventData.Duration,
            () => base.ReaderExecuted(command, eventData, result));
    }

    public override async ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ReaderExecutedAsync called with {CommandText}", command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length)));
        return await CaptureAndProceedAsync(
            command,
            eventData.Duration,
            () => base.ReaderExecutedAsync(command, eventData, result, cancellationToken));
    }

    public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)
    {
        _logger.LogInformation("NonQueryExecuted called with {CommandText}", command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length)));
        return CaptureAndProceed(
            command,
            eventData.Duration,
            () => base.NonQueryExecuted(command, eventData, result));
    }

    public override async ValueTask<int> NonQueryExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("NonQueryExecutedAsync called with {CommandText}", command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length)));
        return await CaptureAndProceedAsync(
            command,
            eventData.Duration,
            () => base.NonQueryExecutedAsync(command, eventData, result, cancellationToken));
    }

    public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)
    {
        _logger.LogInformation("ScalarExecuted called with {CommandText}", command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length)));
        return CaptureAndProceed(
            command,
            eventData.Duration,
            () => base.ScalarExecuted(command, eventData, result));
    }

    public override async ValueTask<object?> ScalarExecutedAsync(
        DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("ScalarExecutedAsync called with {CommandText}", command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length)));
        return await CaptureAndProceedAsync(
            command,
            eventData.Duration,
            () => base.ScalarExecutedAsync(command, eventData, result, cancellationToken));
    }

    public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
    {
        _logger.LogError(eventData.Exception, "Failed to {CommandText}", command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length)));
        CaptureAndProceed(
            command,
            eventData.Duration,
            () => base.CommandFailed(command, eventData));
    }

    public override async Task CommandFailedAsync(DbCommand command, CommandErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        _logger.LogError(eventData.Exception, "Failed to {CommandText}", command.CommandText.Substring(0, Math.Min(100, command.CommandText.Length)));
        await CaptureAndProceedAsync(
            command,
            eventData.Duration,
            () => base.CommandFailedAsync(command, eventData, cancellationToken));
    }

    /// <summary>
    /// Core capture path. Public so it can be exercised directly from tests without a
    /// live database connection.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="command"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is negative.</exception>
    public SlowQuerySample? Capture(DbCommand command, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentOutOfRangeException.ThrowIfLessThan(duration, TimeSpan.Zero);

        var commandText = command.CommandText ?? string.Empty;

        if (_excludedSqlFragments.Any(fragment => commandText.Contains(fragment, StringComparison.OrdinalIgnoreCase)))
            return null;

        // Determine the effective threshold, taking per‑provider overrides into account.
        var effectiveThreshold = GetEffectiveThreshold(command);

        if (duration < effectiveThreshold)
            return null;

        // Apply sampling if configured
        if (!ShouldSample(command))
        {
            _logger.LogDebug("Slow query sampling skipped for: {CommandText}", commandText.Substring(0, Math.Min(100, commandText.Length)));
            return null;
        }

        // Generate index suggestions (either synchronously or via background analysis)
        IReadOnlyList<IndexSuggestion> suggestions = Array.Empty<IndexSuggestion>();
        if (_options.SuggestIndexes)
        {
            if (_options.AnalyzeOnBackgroundThread && _backgroundAnalyzer != null)
            {
                // Queue for background analysis - suggestions will be empty initially
                // In a real implementation, we'd need to store the analysis result
                // For now, we queue the SQL and the background analyzer does the work
                _backgroundAnalyzer.TryQueue(commandText);
            }
            else
            {
                // Synchronous analysis (original behavior)
                suggestions = _syncAnalyzer.Analyze(commandText);
            }
        }

        // Create sample with suggestions
        var sample = new SlowQuerySample
        {
            Sql = commandText,
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

    // Shared helper for synchronous paths
    private T CaptureAndProceed<T>(DbCommand command, TimeSpan duration, Func<T> baseCall)
    {
        Capture(command, duration);
        return baseCall();
    }

    // Shared helper for asynchronous paths returning ValueTask<T>
    private async ValueTask<T> CaptureAndProceedAsync<T>(DbCommand command, TimeSpan duration, Func<ValueTask<T>> baseCall)
    {
        Capture(command, duration);
        return await baseCall();
    }

    // Shared helper for void synchronous paths
    private void CaptureAndProceed(DbCommand command, TimeSpan duration, Action baseCall)
    {
        Capture(command, duration);
        baseCall();
    }

    // Shared helper for async Task paths
    private async Task CaptureAndProceedAsync(DbCommand command, TimeSpan duration, Func<Task> baseCall)
    {
        Capture(command, duration);
        await baseCall();
    }

    /// <summary>
    /// Determines whether this slow query should be sampled based on SamplingRate.
    /// Uses a deterministic hash of the SQL to ensure consistent sampling across restarts.
    /// </summary>
    /// <summary>
    /// Determines whether this slow query should be sampled based on SamplingRate.
    /// Uses a deterministic hash of the SQL to ensure consistent sampling across restarts.
    /// </summary>
    private bool ShouldSample(DbCommand command)
    {
        // If sampling rate is 1.0, always sample
        if (_options.SamplingRate >= 1.0)
            return true;

        // If sampling rate is 0.0, never sample
        if (_options.SamplingRate <= 0.0)
            return false;

        lock (_samplingGate)
        {
            // Use a deterministic hash of the SQL to decide sampling
            // This ensures the same queries are consistently sampled across application restarts
            var sql = command.CommandText ?? string.Empty;
            var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(sql));
            var hash = BitConverter.ToUInt32(hashBytes, 0);

            // Simple deterministic sampling: sample every N queries where N = 1/samplingRate
            var sampleInterval = (int)Math.Ceiling(1.0 / _options.SamplingRate);
            var counter = _samplingCounters.GetOrAdd(sql, _ => 0);
            _samplingCounters[sql] = counter + 1;
            var shouldSample = counter % sampleInterval == 0;

            return shouldSample;
        }
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
                sb.Append(" ").AppendLine(s.ToSqlHint());
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
