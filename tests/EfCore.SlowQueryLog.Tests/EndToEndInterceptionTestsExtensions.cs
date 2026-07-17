using System.Data.Common;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Microsoft.Data.Sqlite;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Extension methods for <see cref="EndToEndInterceptionTests"/> that provide utility functionality
/// for testing EF Core slow query interception scenarios.
/// </summary>
public static class EndToEndInterceptionTestsExtensions
{
    /// <summary>
    /// Creates a new in-memory SQLite database connection suitable for testing.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <returns>A new SQLite connection.</returns>
    public static SqliteConnection CreateInMemoryConnection(this EndToEndInterceptionTests test)
        => new SqliteConnection("DataSource=:memory:");

    /// <summary>
    /// Creates a new slow query interceptor with the specified threshold.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="threshold">The threshold for considering a query slow.</param>
    /// <returns>A new SlowQueryInterceptor instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="threshold"/> is negative or zero.</exception>
    public static SlowQueryInterceptor CreateSlowQueryInterceptor(
        this EndToEndInterceptionTests test,
        TimeSpan threshold)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(threshold, TimeSpan.Zero);
        return new SlowQueryInterceptor(new SlowQueryLogOptions { Threshold = threshold });
    }

    /// <summary>
    /// Creates a new slow query interceptor with a default threshold of 1 tick.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <returns>A new SlowQueryInterceptor instance.</returns>
    public static SlowQueryInterceptor CreateDefaultSlowQueryInterceptor(this EndToEndInterceptionTests test)
        => new SlowQueryInterceptor(new SlowQueryLogOptions { Threshold = TimeSpan.FromTicks(1) });

    /// <summary>
    /// Gets the slow query samples recorded by the interceptor.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="interceptor">The interceptor to get samples from.</param>
    /// <returns>An immutable list of slow query samples.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="interceptor"/> is null.</exception>
    public static IReadOnlyList<SlowQuerySample> GetSlowQuerySamples(
        this EndToEndInterceptionTests test,
        SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        return interceptor.Ranking.Snapshot();
    }

    /// <summary>
    /// Gets the count of slow queries recorded by the interceptor.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="interceptor">The interceptor to check.</param>
    /// <returns>The number of slow queries recorded.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="interceptor"/> is null.</exception>
    public static int GetSlowQueryCount(
        this EndToEndInterceptionTests test,
        SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        return interceptor.Ranking.Count;
    }

    /// <summary>
    /// Clears all recorded slow queries from the interceptor.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="interceptor">The interceptor to clear.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="interceptor"/> is null.</exception>
    public static void ClearSlowQueries(
        this EndToEndInterceptionTests test,
        SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        interceptor.Ranking.Clear();
    }

    /// <summary>
    /// Captures a slow query directly from a command and duration without requiring a full database context.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="interceptor">The interceptor to use.</param>
    /// <param name="commandText">The SQL command text.</param>
    /// <param name="duration">The execution duration.</param>
    /// <returns>The captured slow query sample, or null if the query was not slow.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="interceptor"/> or <paramref name="commandText"/> is null.</exception>
    public static SlowQuerySample? CaptureSlowQuery(
        this EndToEndInterceptionTests test,
        SlowQueryInterceptor interceptor,
        string commandText,
        TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        ArgumentNullException.ThrowIfNull(commandText);

        var command = new Microsoft.Data.Sqlite.SqliteCommand { CommandText = commandText };
        return interceptor.Capture(command, duration);
    }
}