using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace EfCore.SlowQueryLog.Options;

/// <summary>
/// Provides extension methods for <see cref="SlowQueryLogOptions"/> that enable fluent configuration
/// patterns for setting up slow query logging thresholds, log levels, and behavior options.
/// </summary>
/// <remarks>
/// All extension methods validate their arguments and throw appropriate exceptions for invalid inputs.
/// Methods return the configured <see cref="SlowQueryLogOptions"/> instance to enable method chaining.
/// </remarks>
public static class SlowQueryLogOptionsExtensions
{
    /// <summary>
    /// Configures the slow query execution threshold to the specified number of milliseconds.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="milliseconds">The threshold value in milliseconds. Must be a positive integer.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="milliseconds"/> is not positive.</exception>
    public static SlowQueryLogOptions WithThresholdMilliseconds(this SlowQueryLogOptions options, int milliseconds)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (milliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(milliseconds), "Milliseconds must be positive.");
        }

        options.Threshold = TimeSpan.FromMilliseconds(milliseconds);
        return options;
    }

    /// <summary>
    /// Configures the slow query execution threshold to the specified number of seconds.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="seconds">The threshold value in seconds. Must be a positive integer.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="seconds"/> is not positive.</exception>
    public static SlowQueryLogOptions WithThresholdSeconds(this SlowQueryLogOptions options, int seconds)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (seconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(seconds), "Seconds must be positive.");
        }

        options.Threshold = TimeSpan.FromSeconds(seconds);
        return options;
    }

    /// <summary>
    /// Configures the slow query execution threshold to the specified number of minutes.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="minutes">The threshold value in minutes. Must be a positive integer.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="minutes"/> is not positive.</exception>
    public static SlowQueryLogOptions WithThresholdMinutes(this SlowQueryLogOptions options, int minutes)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (minutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minutes), "Minutes must be positive.");
        }

        options.Threshold = TimeSpan.FromMinutes(minutes);
        return options;
    }

    /// <summary>
    /// Configures the logging level to the specified value.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="level">The <see cref="LogLevel"/> to use for logging.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static SlowQueryLogOptions WithLogLevel(this SlowQueryLogOptions options, LogLevel level)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.LogLevel = level;
        return options;
    }

    /// <summary>
    /// Enables logging of parameter values for slow queries.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static SlowQueryLogOptions WithParameterValues(this SlowQueryLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.IncludeParameterValues = true;
        return options;
    }

    /// <summary>
    /// Disables logging of parameter values for slow queries.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static SlowQueryLogOptions WithoutParameterValues(this SlowQueryLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.IncludeParameterValues = false;
        return options;
    }

    /// <summary>
    /// Enables generation of index suggestions for slow queries.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static SlowQueryLogOptions WithIndexSuggestions(this SlowQueryLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.SuggestIndexes = true;
        return options;
    }

    /// <summary>
    /// Disables generation of index suggestions for slow queries.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static SlowQueryLogOptions WithoutIndexSuggestions(this SlowQueryLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.SuggestIndexes = false;
        return options;
    }

    /// <summary>
    /// Configures the maximum number of slow queries to retain in the ranking.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="capacity">The maximum number of slow queries to retain in the ranking. Must be a positive integer.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="capacity"/> is not positive.</exception>
    public static SlowQueryLogOptions WithRankingCapacity(this SlowQueryLogOptions options, int capacity)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be positive.");
        }

        options.RankingCapacity = capacity;
        return options;
    }

    /// <summary>
    /// Configures the maximum number of slow query samples to retain in memory.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="maxSamples">The maximum number of samples to retain. Must be a positive integer.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="maxSamples"/> is not positive.</exception>
    public static SlowQueryLogOptions WithMaxSamples(this SlowQueryLogOptions options, int maxSamples)
    {
        ArgumentNullException.ThrowIfNull(options);
        if (maxSamples <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxSamples), "MaxSamples must be positive.");
        }

        options.MaxSamples = maxSamples;
        return options;
    }

    /// <summary>
    /// Sets a callback that is invoked for every slow query.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="callback">The callback to invoke for slow queries.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/>.</exception>
    public static SlowQueryLogOptions WithOnSlowQuery(this SlowQueryLogOptions options, Action<SlowQuerySample>? callback)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.OnSlowQuery = callback;
        return options;
    }

    /// <summary>
    /// Configures multiple options at once using an action delegate.
    /// </summary>
    /// <param name="options">The options instance to configure. Cannot be <see langword="null"/>.</param>
    /// <param name="configure">An action that applies configuration to the options. Cannot be null.</param>
    /// <returns>The configured <see cref="SlowQueryLogOptions"/> instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">
    /// <paramref name="options"/> is <see langword="null"/> or
    /// <paramref name="configure"/> is <see langword="null"/>
    /// </exception>
    public static SlowQueryLogOptions Configure(this SlowQueryLogOptions options, Action<SlowQueryLogOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(configure);

        configure(options);
        return options;
    }

    /// <summary>
    /// Creates a new <see cref="SlowQueryLogOptions"/> instance with default values.
    /// </summary>
    /// <returns>A new instance with default configuration.</returns>
    public static SlowQueryLogOptions CreateDefault() => new();

    /// <summary>
    /// Creates a new <see cref="SlowQueryLogOptions"/> instance configured with common development settings.
    /// </summary>
    /// <param name="thresholdMilliseconds">The slow query execution threshold in milliseconds. Defaults to 200ms.</param>
    /// <returns>A new instance configured for development use.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="thresholdMilliseconds"/> is not positive.</exception>
    public static SlowQueryLogOptions CreateDevelopment(int thresholdMilliseconds = 200)
    {
        if (thresholdMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdMilliseconds), "Threshold must be positive.");
        }

        return new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(thresholdMilliseconds),
            LogLevel = LogLevel.Debug,
            IncludeParameterValues = false,
            SuggestIndexes = true,
            RankingCapacity = 50,
            MaxSamples = 1000,
            OnSlowQuery = null
        };
    }

    /// <summary>
    /// Creates a new <see cref="SlowQueryLogOptions"/> instance configured for production monitoring.
    /// </summary>
    /// <param name="thresholdMilliseconds">The slow query execution threshold in milliseconds. Defaults to 500ms.</param>
    /// <returns>A new instance configured for production use.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="thresholdMilliseconds"/> is not positive.</exception>
    public static SlowQueryLogOptions CreateProduction(int thresholdMilliseconds = 500)
    {
        if (thresholdMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdMilliseconds), "Threshold must be positive.");
        }

        return new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(thresholdMilliseconds),
            LogLevel = LogLevel.Warning,
            IncludeParameterValues = false,
            SuggestIndexes = true,
            RankingCapacity = 100,
            MaxSamples = 2000,
            OnSlowQuery = null
        };
    }

    /// <summary>
    /// Creates a new <see cref="SlowQueryLogOptions"/> instance configured for debugging with full details.
    /// </summary>
    /// <param name="thresholdMilliseconds">The slow query execution threshold in milliseconds. Defaults to 100ms.</param>
    /// <returns>A new instance configured for debugging.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="thresholdMilliseconds"/> is not positive.</exception>
    public static SlowQueryLogOptions CreateDebug(int thresholdMilliseconds = 100)
    {
        if (thresholdMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdMilliseconds), "Threshold must be positive.");
        }

        return new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(thresholdMilliseconds),
            LogLevel = LogLevel.Information,
            IncludeParameterValues = true,
            SuggestIndexes = true,
            RankingCapacity = 200,
            MaxSamples = 5000,
            OnSlowQuery = null
        };
    }

    /// <summary>
    /// Creates a new <see cref="SlowQueryLogOptions"/> instance configured to capture all queries for analysis.
    /// </summary>
    /// <param name="thresholdMilliseconds">The slow query execution threshold in milliseconds. Defaults to 1ms.</param>
    /// <returns>A new instance configured to capture all queries.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="thresholdMilliseconds"/> is not positive.</exception>
    public static SlowQueryLogOptions CreateCaptureAll(int thresholdMilliseconds = 1)
    {
        if (thresholdMilliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(thresholdMilliseconds), "Threshold must be positive.");
        }

        return new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(thresholdMilliseconds),
            LogLevel = LogLevel.Trace,
            IncludeParameterValues = true,
            SuggestIndexes = true,
            RankingCapacity = 1000,
            MaxSamples = 10000,
            OnSlowQuery = null
        };
    }

}
