using System.Data.Common;
using EfCore.SlowQueryLog.Reporting;

namespace EfCore.SlowQueryLog.Interception;

/// <summary>
/// Provides extension methods for <see cref="SlowQueryInterceptor"/> to simplify common operations
/// such as clearing the ranking, checking if queries have been captured, and retrieving
/// aggregated statistics.
/// </summary>
public static class SlowQueryInterceptorExtensions
{
    /// <summary>
    /// Clears all captured slow queries from the interceptor's ranking.
    /// </summary>
    /// <param name="interceptor">The <see cref="SlowQueryInterceptor"/> instance.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="interceptor"/> is null.</exception>
    public static void Clear(this SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        interceptor.Ranking.Clear();
    }

    /// <summary>
    /// Determines whether any slow queries have been captured by the interceptor.
    /// </summary>
    /// <param name="interceptor">The <see cref="SlowQueryInterceptor"/> instance.</param>
    /// <returns>True if at least one slow query has been captured; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="interceptor"/> is null.</exception>
    public static bool HasCapturedQueries(this SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        return interceptor.Ranking.Count > 0;
    }

    /// <summary>
    /// Gets the total number of slow queries currently captured by the interceptor.
    /// </summary>
    /// <param name="interceptor">The <see cref="SlowQueryInterceptor"/> instance.</param>
    /// <returns>The count of captured slow queries.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="interceptor"/> is null.</exception>
    public static int GetQueryCount(this SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        return interceptor.Ranking.Count;
    }

    /// <summary>
    /// Gets a snapshot of all captured slow queries, ordered by duration (slowest first).
    /// </summary>
    /// <param name="interceptor">The <see cref="SlowQueryInterceptor"/> instance.</param>
    /// <returns>An <see cref="IReadOnlyList{T}"/> of <see cref="SlowQuerySample"/> instances.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="interceptor"/> is null.</exception>
    public static IReadOnlyList<SlowQuerySample> GetCapturedQueries(this SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        return interceptor.Ranking.Snapshot();
    }

    /// <summary>
    /// Gets the slowest captured query (the query with the longest duration).
    /// </summary>
    /// <param name="interceptor">The <see cref="SlowQueryInterceptor"/> instance.</param>
    /// <returns>The <see cref="SlowQuerySample"/> with the longest duration, or null if no queries have been captured.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="interceptor"/> is null.</exception>
    public static SlowQuerySample? GetSlowestQuery(this SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        var snapshot = interceptor.Ranking.Snapshot();
        return snapshot.Count == 0 ? null : snapshot[0];
    }

    /// <summary>
    /// Gets the fastest captured query (the query with the shortest duration among slow queries).
    /// </summary>
    /// <param name="interceptor">The <see cref="SlowQueryInterceptor"/> instance.</param>
    /// <returns>The <see cref="SlowQuerySample"/> with the shortest duration, or null if no queries have been captured.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="interceptor"/> is null.</exception>
    public static SlowQuerySample? GetFastestQuery(this SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        var snapshot = interceptor.Ranking.Snapshot();
        return snapshot.Count == 0 ? null : snapshot[^1];
    }

    /// <summary>
    /// Captures a slow query directly from a <see cref="DbCommand"/> and duration,
    /// bypassing the normal interceptor execution path.
    /// </summary>
    /// <param name="interceptor">The <see cref="SlowQueryInterceptor"/> instance.</param>
    /// <param name="command">The <see cref="DbCommand"/> that was executed.</param>
    /// <param name="duration">The execution duration.</param>
    /// <returns>The captured <see cref="SlowQuerySample"/>, or null if the duration was below the threshold.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if <paramref name="interceptor"/> or <paramref name="command"/> is null.
    /// </exception>
    public static SlowQuerySample? Capture(this SlowQueryInterceptor interceptor, DbCommand command, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        ArgumentNullException.ThrowIfNull(command);
        return interceptor.Capture(command, duration);
    }

    /// <summary>
    /// Gets the total duration of all captured slow queries.
    /// </summary>
    /// <param name="interceptor">The <see cref="SlowQueryInterceptor"/> instance.</param>
    /// <returns>A <see cref="TimeSpan"/> representing the cumulative duration of all captured queries.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="interceptor"/> is null.</exception>
    public static TimeSpan GetTotalDuration(this SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        return interceptor.Ranking.GetTotalDuration();
    }

    /// <summary>
    /// Gets the average duration of all captured slow queries in milliseconds.
    /// </summary>
    /// <param name="interceptor">The <see cref="SlowQueryInterceptor"/> instance.</param>
    /// <returns>The average duration in milliseconds, or 0.0 if no queries have been captured.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="interceptor"/> is null.</exception>
    public static double GetAverageDurationMs(this SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        return interceptor.Ranking.GetAverageDuration();
    }

    /// <summary>
    /// Gets all index suggestions from all captured queries.
    /// </summary>
    /// <param name="interceptor">The <see cref="SlowQueryInterceptor"/> instance.</param>
    /// <returns>An <see cref="IEnumerable{IndexSuggestion}"/> containing all suggestions across all queries.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="interceptor"/> is null.</exception>
    public static IEnumerable<IndexSuggestion> GetAllIndexSuggestions(this SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(interceptor);
        return interceptor.Ranking.GetAllSuggestions();
    }
}