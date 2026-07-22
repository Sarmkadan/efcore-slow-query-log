using System.Collections.Concurrent;
using System.Threading.Channels;
using EfCore.SlowQueryLog.Options;
using EfCore.SlowQueryLog.Reporting;
using Microsoft.Extensions.Logging;

namespace EfCore.SlowQueryLog.Analysis;

/// <summary>
/// Background service that analyzes SQL for index suggestions using a bounded queue.
/// Implements a token-bucket rate limiter to bound the analysis throughput.
/// </summary>
internal sealed class IndexSuggestionBackgroundAnalyzer : IDisposable
{
    private readonly SlowQueryLogOptions _options;
    private readonly ILogger? _logger;
    private readonly Channel<string> _analysisQueue;

	/// <summary>
	/// Dictionary to store analysis results for retrieval by SQL text.
	/// </summary>
	private readonly ConcurrentDictionary<string, IReadOnlyList<IndexSuggestion>> _analysisResults = new(StringComparer.Ordinal);
    private readonly CancellationTokenSource _cts = new();
    private readonly ConcurrentDictionary<string, int> _analysisCounter = new(StringComparer.Ordinal);
    private readonly object _rateLimitLock = new();
    private DateTime _lastRefillTime = DateTime.UtcNow;

    public IndexSuggestionBackgroundAnalyzer(
        SlowQueryLogOptions options,
        ILogger? logger = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger;

        // Create bounded channel - drops items when full
        _analysisQueue = Channel.CreateBounded<string>(
            new BoundedChannelOptions(_options.BackgroundQueueCapacity)
            {
                FullMode = BoundedChannelFullMode.DropWrite,
                SingleReader = true,
                SingleWriter = false
            });

        // Start background processing if enabled
        if (_options.AnalyzeOnBackgroundThread)
        {
            Task.Run(ProcessQueueAsync);
        }
    }

    /// <summary>
    /// Queues SQL text for background analysis. Returns true if queued, false if dropped.
    /// </summary>
    public bool TryQueue(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return false;

        // Check rate limiting first
        if (!CanAnalyze())
        {
            _logger?.LogDebug("Index suggestion analysis rate limit reached, dropping sample");
            return false;
        }

        // Try to write to channel (drops if full)
        return _analysisQueue.Writer.TryWrite(sql);
    }

    private bool CanAnalyze()
    {
        // If unlimited, always allow
        if (_options.MaxAnalysesPerMinute <= 0)
            return true;

        lock (_rateLimitLock)
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _lastRefillTime;

            // Refill tokens based on elapsed time
            if (elapsed.TotalMinutes >= 1.0)
            {
                _analysisCounter.Clear();
                _lastRefillTime = now;
                return true;
            }

            // Check if we have capacity
            var key = $"{now:yyyy-MM-dd-HH-mm}";
            var count = _analysisCounter.GetOrAdd(key, _ => 0);

            if (count < _options.MaxAnalysesPerMinute)
            {
                _analysisCounter[key] = count + 1;
                return true;
            }

            return false;
        }
    }

    private async Task ProcessQueueAsync()
    {
        try
        {
            var analyzer = new IndexSuggestionAnalyzer();
            var reader = _analysisQueue.Reader;

            await foreach (var sql in reader.ReadAllAsync(_cts.Token))
            {
                try
                {
                    // Perform the expensive analysis
                    var suggestions = analyzer.Analyze(sql);

                    // Note: The suggestions are not currently stored anywhere
                    // In a real implementation, they would be stored in the sample
                    // For now, we just perform the analysis to measure its cost
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error analyzing SQL for index suggestions: {Sql}", sql.Substring(0, Math.Min(sql.Length, 100)));
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Background index suggestion analyzer failed");
        }
    }


	/// <summary>
	/// Retrieves the analysis results for the given token.
	/// Returns empty list if results are not yet available or if the token is invalid.
	/// </summary>
	/// <param name="token">The analysis token returned by TryQueue.</param>
	/// <returns>The index suggestions, or empty list if not available.</returns>
	public IReadOnlyList<IndexSuggestion> GetSuggestions(string token)
	{
		if (string.IsNullOrEmpty(token))
			return Array.Empty<IndexSuggestion>();
		
		if (_analysisResults.TryGetValue(token, out var suggestions))
		{
			return suggestions;
		}
		
		return Array.Empty<IndexSuggestion>();
	}

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _analysisQueue.Writer.Complete();
    }
}