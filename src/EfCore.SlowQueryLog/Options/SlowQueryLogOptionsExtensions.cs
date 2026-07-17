using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace EfCore.SlowQueryLog.Options;

/// <summary>
/// Extension methods for <see cref="SlowQueryLogOptions"/> that provide convenient
/// configuration patterns and fluent APIs.
/// </summary>
public static class SlowQueryLogOptionsExtensions
{
    /// <summary>
    /// Configures the threshold to the specified milliseconds.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="milliseconds">The threshold in milliseconds.</param>
    /// <returns>The configured options instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
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
    /// Configures the threshold to the specified seconds.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="seconds">The threshold in seconds.</param>
    /// <returns>The configured options instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
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
    /// Configures the threshold to the specified minutes.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="minutes">The threshold in minutes.</param>
    /// <returns>The configured options instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
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
    /// Configures the log level to the specified value.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="level">The log level to use.</param>
    /// <returns>The configured options instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    public static SlowQueryLogOptions WithLogLevel(this SlowQueryLogOptions options, LogLevel level)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.LogLevel = level;
        return options;
    }

    /// <summary>
    /// Enables parameter value logging for the slow query interceptor.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <returns>The configured options instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    public static SlowQueryLogOptions WithParameterValues(this SlowQueryLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.IncludeParameterValues = true;
        return options;
    }

    /// <summary>
    /// Disables parameter value logging for the slow query interceptor.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <returns>The configured options instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    public static SlowQueryLogOptions WithoutParameterValues(this SlowQueryLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.IncludeParameterValues = false;
        return options;
    }

    /// <summary>
    /// Enables index suggestion analysis for slow queries.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <returns>The configured options instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    public static SlowQueryLogOptions WithIndexSuggestions(this SlowQueryLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.SuggestIndexes = true;
        return options;
    }

    /// <summary>
    /// Disables index suggestion analysis for slow queries.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <returns>The configured options instance for fluent changing.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    public static SlowQueryLogOptions WithoutIndexSuggestions(this SlowQueryLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.SuggestIndexes = false;
        return options;
    }

    /// <summary>
    /// Configures the ranking capacity to the specified value.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="capacity">The maximum number of slow queries to retain in the ranking.</param>
    /// <returns>The configured options instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
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
    /// Sets the callback that is invoked for every slow query.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="callback">The callback to invoke for slow queries.</param>
    /// <returns>The configured options instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="options"/> is <see langword="null"/></exception>
    public static SlowQueryLogOptions WithOnSlowQuery(this SlowQueryLogOptions options, Action<SlowQuerySample>? callback)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.OnSlowQuery = callback;
        return options;
    }

    /// <summary>
    /// Configures multiple options at once using an action delegate.
    /// </summary>
    /// <param name="options">The options instance to configure.</param>
    /// <param name="configure">An action that applies configuration to the options.</param>
    /// <returns>The configured options instance for fluent chaining.</returns>
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
    /// <param name="thresholdMilliseconds">The threshold in milliseconds. Defaults to 200ms.</param>
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
            OnSlowQuery = null
        };
    }

    /// <summary>
    /// Creates a new <see cref="SlowQueryLogOptions"/> instance configured for production monitoring.
    /// </summary>
    /// <param name="thresholdMilliseconds">The threshold in milliseconds. Defaults to 500ms.</param>
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
            OnSlowQuery = null
        };
    }

    /// <summary>
    /// Creates a new <see cref="SlowQueryLogOptions"/> instance configured for debugging with full details.
    /// </summary>
    /// <param name="thresholdMilliseconds">The threshold in milliseconds. Defaults to 100ms.</param>
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
            OnSlowQuery = null
        };
    }

    /// <summary>
    /// Creates a new <see cref="SlowQueryLogOptions"/> instance configured to capture all queries for analysis.
    /// </summary>
    /// <param name="thresholdMilliseconds">The threshold in milliseconds. Defaults to 1ms.</param>
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
            OnSlowQuery = null
        };
    }

}
