using System;
using System.Collections.Generic;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Microsoft.Data.Sqlite;

namespace EfCore.SlowQueryLog.Tests;

public static class SlowQueryInterceptorTestsExtensions
{
    /// <summary>
    /// Creates a test command with the specified SQL and parameters.
    /// </summary>
    /// <param name="sql">The SQL command text.</param>
    /// <param name="ps">Named parameters with their values.</param>
    /// <returns>A configured SqliteCommand instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when sql is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when name is null in parameters array.</exception>
    public static SqliteCommand CreateTestCommand(this string sql, params (string name, object value)[] ps)
    {
        ArgumentNullException.ThrowIfNull(sql);

        var cmd = new SqliteCommand { CommandText = sql };
        foreach (var (name, value) in ps)
        {
            ArgumentNullException.ThrowIfNull(name);
            cmd.Parameters.AddWithValue(name, value);
        }
        return cmd;
    }

    /// <summary>
    /// Creates a SlowQueryInterceptor with default test configuration.
    /// </summary>
    /// <param name="threshold">The threshold for slow queries.</param>
    /// <param name="includeParameters">Whether to include parameter values.</param>
    /// <param name="suggestIndexes">Whether to generate index suggestions.</param>
    /// <returns>A configured SlowQueryInterceptor instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is not positive.</exception>
    public static SlowQueryInterceptor CreateTestInterceptor(
        this TimeSpan threshold,
        bool includeParameters = false,
        bool suggestIndexes = true)
    {
        return new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = threshold,
            IncludeParameterValues = includeParameters,
            SuggestIndexes = suggestIndexes
        });
    }

    /// <summary>
    /// Creates a SlowQueryInterceptor with a callback for testing notification scenarios.
    /// </summary>
    /// <param name="threshold">The threshold for slow queries.</param>
    /// <param name="onSlowQuery">Callback invoked when a slow query is detected.</param>
    /// <param name="includeParameters">Whether to include parameter values.</param>
    /// <param name="suggestIndexes">Whether to generate index suggestions.</param>
    /// <returns>A configured SlowQueryInterceptor instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when onSlowQuery is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when threshold is not positive.</exception>
    public static SlowQueryInterceptor CreateTestInterceptor(
        this TimeSpan threshold,
        Action<SlowQuerySample> onSlowQuery,
        bool includeParameters = false,
        bool suggestIndexes = true)
    {
        ArgumentNullException.ThrowIfNull(onSlowQuery);

        return new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = threshold,
            IncludeParameterValues = includeParameters,
            SuggestIndexes = suggestIndexes,
            OnSlowQuery = onSlowQuery
        });
    }

    /// <summary>
    /// Captures a slow query and returns the sample for further assertions.
    /// </summary>
    /// <param name="interceptor">The interceptor instance.</param>
    /// <param name="sql">The SQL command text.</param>
    /// <param name="executionTime">The actual execution time.</param>
    /// <param name="ps">Named parameters with their values.</param>
    /// <returns>The captured SlowQuerySample, or null if the query was not slow.</returns>
    /// <exception cref="ArgumentNullException">Thrown when interceptor or sql is null.</exception>
    public static SlowQuerySample? CaptureSlowQuery(
        this SlowQueryInterceptor interceptor,
        string sql,
        TimeSpan executionTime,
        params (string name, object value)[] ps)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        ArgumentNullException.ThrowIfNull(sql);

        var cmd = sql.CreateTestCommand(ps);
        return interceptor.Capture(cmd, executionTime);
    }

    /// <summary>
    /// Gets all captured slow query samples from the interceptor's ranking.
    /// </summary>
    /// <param name="interceptor">The interceptor instance.</param>
    /// <returns>An enumerable of captured SlowQuerySample instances.</returns>
    /// <exception cref="ArgumentNullException">Thrown when interceptor is null.</exception>
    public static IEnumerable<SlowQuerySample> GetCapturedSamples(this SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        return interceptor.Ranking.Snapshot();
    }

    /// <summary>
    /// Gets the number of slow queries captured by the interceptor.
    /// </summary>
    /// <param name="interceptor">The interceptor instance.</param>
    /// <returns>The count of captured slow queries.</returns>
    /// <exception cref="ArgumentNullException">Thrown when interceptor is null.</exception>
    public static int GetSlowQueryCount(this SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        return interceptor.Ranking.Count;
    }

    /// <summary>
    /// Creates a test interceptor with a very low threshold for testing capture behavior.
    /// </summary>
    /// <param name="includeParameters">Whether to include parameter values.</param>
    /// <param name="suggestIndexes">Whether to generate index suggestions.</param>
    /// <returns>A configured SlowQueryInterceptor instance with 1ms threshold.</returns>
    public static SlowQueryInterceptor CreateAlwaysCapturingInterceptor(
        bool includeParameters = false,
        bool suggestIndexes = true)
    {
        return TimeSpan.FromMilliseconds(1).CreateTestInterceptor(
            includeParameters,
            suggestIndexes);
    }
}