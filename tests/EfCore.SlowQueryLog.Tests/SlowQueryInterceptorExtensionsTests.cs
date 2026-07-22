using System.Data.Common;
using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Microsoft.Data.Sqlite;
using Xunit;

/// <summary>
/// Tests for the SlowQueryInterceptorExtensions class.
/// </summary>
public class SlowQueryInterceptorExtensionsTests
{
    /// <summary>
    /// Creates a new SqliteCommand with the given SQL and parameters.
    /// </summary>
    private static SqliteCommand Command(string sql, params (string name, object value)[] ps)
    {
        var cmd = new SqliteCommand { CommandText = sql };
        foreach (var (name, value) in ps)
            cmd.Parameters.AddWithValue(name, value);
        cmd.Connection = new SqliteConnection("Data Source=:memory:");
        return cmd;
    }

    /// <summary>
    /// Verifies that Clear() removes all captured queries.
    /// </summary>
    [Fact]
    public void Clear_removes_all_captured_queries()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
        });

        // Add some queries
        interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(100));
        interceptor.Capture(Command("SELECT 2"), TimeSpan.FromMilliseconds(200));
        interceptor.Capture(Command("SELECT 3"), TimeSpan.FromMilliseconds(300));

        Assert.Equal(3, interceptor.GetQueryCount());
        Assert.True(interceptor.HasCapturedQueries());

        // Clear should remove all queries
        interceptor.Clear();

        Assert.Equal(0, interceptor.GetQueryCount());
        Assert.False(interceptor.HasCapturedQueries());
    }

    /// <summary>
    /// Verifies that Clear() throws ArgumentNullException for null interceptor.
    /// </summary>
    [Fact]
    public void Clear_throws_for_null_interceptor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ((SlowQueryInterceptor)null!).Clear());
        Assert.Equal("interceptor", ex.ParamName);
    }

    /// <summary>
    /// Verifies that HasCapturedQueries() returns false when no queries are captured.
    /// </summary>
    [Fact]
    public void HasCapturedQueries_returns_false_when_empty()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1000), // High threshold so nothing is captured
        });

        Assert.False(interceptor.HasCapturedQueries());
    }

    /// <summary>
    /// Verifies that HasCapturedQueries() returns true when queries are captured.
    /// </summary>
    [Fact]
    public void HasCapturedQueries_returns_true_when_has_queries()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
        });

        interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(100));

        Assert.True(interceptor.HasCapturedQueries());
    }

    /// <summary>
    /// Verifies that HasCapturedQueries() throws ArgumentNullException for null interceptor.
    /// </summary>
    [Fact]
    public void HasCapturedQueries_throws_for_null_interceptor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ((SlowQueryInterceptor)null!).HasCapturedQueries());
        Assert.Equal("interceptor", ex.ParamName);
    }

    /// <summary>
    /// Verifies that GetQueryCount() returns 0 when no queries are captured.
    /// </summary>
    [Fact]
    public void GetQueryCount_returns_zero_when_empty()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1000), // High threshold
        });

        Assert.Equal(0, interceptor.GetQueryCount());
    }

    /// <summary>
    /// Verifies that GetQueryCount() returns correct count when queries are captured.
    /// </summary>
    [Fact]
    public void GetQueryCount_returns_correct_count()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
        });

        interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(100));
        interceptor.Capture(Command("SELECT 2"), TimeSpan.FromMilliseconds(200));
        interceptor.Capture(Command("SELECT 3"), TimeSpan.FromMilliseconds(300));

        Assert.Equal(3, interceptor.GetQueryCount());
    }

    /// <summary>
    /// Verifies that GetQueryCount() throws ArgumentNullException for null interceptor.
    /// </summary>
    [Fact]
    public void GetQueryCount_throws_for_null_interceptor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ((SlowQueryInterceptor)null!).GetQueryCount());
        Assert.Equal("interceptor", ex.ParamName);
    }

    /// <summary>
    /// Verifies that GetCapturedQueries() returns empty list when no queries are captured.
    /// </summary>
    [Fact]
    public void GetCapturedQueries_returns_empty_list_when_empty()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1000), // High threshold
        });

        var queries = interceptor.GetCapturedQueries();
        Assert.NotNull(queries);
        Assert.Empty(queries);
    }

    /// <summary>
    /// Verifies that GetCapturedQueries() returns queries ordered by duration (slowest first).
    /// </summary>
    [Fact]
    public void GetCapturedQueries_returns_queries_ordered_by_duration()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
            RankingCapacity = 5,
        });

        // Add queries in random order
        interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(500));
        interceptor.Capture(Command("SELECT 2"), TimeSpan.FromMilliseconds(200));
        interceptor.Capture(Command("SELECT 3"), TimeSpan.FromMilliseconds(1000));
        interceptor.Capture(Command("SELECT 4"), TimeSpan.FromMilliseconds(300));
        interceptor.Capture(Command("SELECT 5"), TimeSpan.FromMilliseconds(1500));

        var queries = interceptor.GetCapturedQueries();
        Assert.Equal(5, queries.Count);
        Assert.Equal(TimeSpan.FromMilliseconds(1500), queries[0].Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(1000), queries[1].Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(500), queries[2].Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(300), queries[3].Duration);
        Assert.Equal(TimeSpan.FromMilliseconds(200), queries[4].Duration);
    }

    /// <summary>
    /// Verifies that GetCapturedQueries() throws ArgumentNullException for null interceptor.
    /// </summary>
    [Fact]
    public void GetCapturedQueries_throws_for_null_interceptor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ((SlowQueryInterceptor)null!).GetCapturedQueries());
        Assert.Equal("interceptor", ex.ParamName);
    }

    /// <summary>
    /// Verifies that GetSlowestQuery() returns null when no queries are captured.
    /// </summary>
    [Fact]
    public void GetSlowestQuery_returns_null_when_empty()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1000), // High threshold
        });

        var slowest = interceptor.GetSlowestQuery();
        Assert.Null(slowest);
    }

    /// <summary>
    /// Verifies that GetSlowestQuery() returns the query with the longest duration.
    /// </summary>
    [Fact]
    public void GetSlowestQuery_returns_slowest_query()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
        });

        interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(500));
        interceptor.Capture(Command("SELECT 2"), TimeSpan.FromMilliseconds(200));
        interceptor.Capture(Command("SELECT 3"), TimeSpan.FromMilliseconds(1000));

        var slowest = interceptor.GetSlowestQuery();
        Assert.NotNull(slowest);
        Assert.Equal(TimeSpan.FromMilliseconds(1000), slowest!.Duration);
        Assert.Equal("SELECT 3", slowest.Sql);
    }

    /// <summary>
    /// Verifies that GetSlowestQuery() throws ArgumentNullException for null interceptor.
    /// </summary>
    [Fact]
    public void GetSlowestQuery_throws_for_null_interceptor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ((SlowQueryInterceptor)null!).GetSlowestQuery());
        Assert.Equal("interceptor", ex.ParamName);
    }

    /// <summary>
    /// Verifies that GetFastestQuery() returns null when no queries are captured.
    /// </summary>
    [Fact]
    public void GetFastestQuery_returns_null_when_empty()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1000), // High threshold
        });

        var fastest = interceptor.GetFastestQuery();
        Assert.Null(fastest);
    }

    /// <summary>
    /// Verifies that GetFastestQuery() returns the query with the shortest duration.
    /// </summary>
    [Fact]
    public void GetFastestQuery_returns_fastest_query()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
        });

        interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(500));
        interceptor.Capture(Command("SELECT 2"), TimeSpan.FromMilliseconds(200));
        interceptor.Capture(Command("SELECT 3"), TimeSpan.FromMilliseconds(1000));

        var fastest = interceptor.GetFastestQuery();
        Assert.NotNull(fastest);
        Assert.Equal(TimeSpan.FromMilliseconds(200), fastest!.Duration);
        Assert.Equal("SELECT 2", fastest.Sql);
    }

    /// <summary>
    /// Verifies that GetFastestQuery() throws ArgumentNullException for null interceptor.
    /// </summary>
    [Fact]
    public void GetFastestQuery_throws_for_null_interceptor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ((SlowQueryInterceptor)null!).GetFastestQuery());
        Assert.Equal("interceptor", ex.ParamName);
    }

    /// <summary>
    /// Verifies that Capture() adds a query to the ranking.
    /// </summary>
    [Fact]
    public void Capture_adds_query_to_ranking()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
        });

        var sample = interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(100));

        Assert.NotNull(sample);
        Assert.Equal(1, interceptor.GetQueryCount());
        Assert.Same(sample, interceptor.GetCapturedQueries()[0]);
    }

    /// <summary>
    /// Verifies that Capture() returns null when duration is below threshold.
    /// </summary>
    [Fact]
    public void Capture_returns_null_when_below_threshold()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(500),
        });

        var sample = interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(100));

        Assert.Null(sample);
        Assert.Equal(0, interceptor.GetQueryCount());
    }

    /// <summary>
    /// Verifies that Capture() throws NullReferenceException for null interceptor (actual behavior).
    /// </summary>
    [Fact]
    public void Capture_throws_for_null_interceptor()
    {
        // The extension method throws NullReferenceException when calling null!.Capture()
        Assert.Throws<NullReferenceException>(() =>
            ((SlowQueryInterceptor)null!).Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(100)));
    }

    /// <summary>
    /// Verifies that Capture() throws NullReferenceException for null command (actual behavior).
    /// </summary>
    [Fact]
    public void Capture_throws_for_null_command()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
        });

        // The interceptor's Capture method doesn't validate null command, it throws NullReferenceException
        Assert.Throws<NullReferenceException>(() =>
            interceptor.Capture(null!, TimeSpan.FromMilliseconds(100)));
    }

    /// <summary>
    /// Verifies that GetAllIndexSuggestions() returns suggestions when queries have them.
    /// </summary>
    [Fact]
    public void GetAllIndexSuggestions_returns_suggestions_when_queries_have_them()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
            SuggestIndexes = true,
        });

        interceptor.Capture(Command("SELECT * FROM Orders WHERE Status = @p0"), TimeSpan.FromMilliseconds(100));

        var suggestions = interceptor.GetAllIndexSuggestions().ToList();
        Assert.NotEmpty(suggestions);
    }

    /// <summary>
    /// Verifies that GetTotalDuration() returns zero when no queries are captured.
    /// </summary>
    [Fact]
    public void GetTotalDuration_returns_zero_when_empty()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1000), // High threshold
        });

        var total = interceptor.GetTotalDuration();
        Assert.Equal(TimeSpan.Zero, total);
    }

    /// <summary>
    /// Verifies that GetTotalDuration() returns the sum of all query durations.
    /// </summary>
    [Fact]
    public void GetTotalDuration_returns_sum_of_durations()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
        });

        interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(100));
        interceptor.Capture(Command("SELECT 2"), TimeSpan.FromMilliseconds(200));
        interceptor.Capture(Command("SELECT 3"), TimeSpan.FromMilliseconds(300));

        var total = interceptor.GetTotalDuration();
        Assert.Equal(TimeSpan.FromMilliseconds(600), total);
    }

    /// <summary>
    /// Verifies that GetTotalDuration() throws ArgumentNullException for null interceptor.
    /// </summary>
    [Fact]
    public void GetTotalDuration_throws_for_null_interceptor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ((SlowQueryInterceptor)null!).GetTotalDuration());
        Assert.Equal("interceptor", ex.ParamName);
    }

    /// <summary>
    /// Verifies that GetAverageDurationMs() returns 0 when no queries are captured.
    /// </summary>
    [Fact]
    public void GetAverageDurationMs_returns_zero_when_empty()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1000), // High threshold
        });

        var avg = interceptor.GetAverageDurationMs();
        Assert.Equal(0.0, avg);
    }

    /// <summary>
    /// Verifies that GetAverageDurationMs() returns the average duration in milliseconds.
    /// </summary>
    [Fact]
    public void GetAverageDurationMs_returns_average_duration()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
        });

        interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(100));
        interceptor.Capture(Command("SELECT 2"), TimeSpan.FromMilliseconds(200));
        interceptor.Capture(Command("SELECT 3"), TimeSpan.FromMilliseconds(300));

        var avg = interceptor.GetAverageDurationMs();
        Assert.Equal(200.0, avg);
    }

    /// <summary>
    /// Verifies that GetAverageDurationMs() throws ArgumentNullException for null interceptor.
    /// </summary>
    [Fact]
    public void GetAverageDurationMs_throws_for_null_interceptor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ((SlowQueryInterceptor)null!).GetAverageDurationMs());
        Assert.Equal("interceptor", ex.ParamName);
    }

    /// <summary>
    /// Verifies that GetAllIndexSuggestions() returns empty enumerable when no queries are captured.
    /// </summary>
    [Fact]
    public void GetAllIndexSuggestions_returns_empty_when_no_queries()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1000), // High threshold
        });

        var suggestions = interceptor.GetAllIndexSuggestions();
        Assert.NotNull(suggestions);
        Assert.Empty(suggestions);
    }

    /// <summary>
    /// Verifies that GetAllIndexSuggestions() returns all suggestions from all queries.
    /// </summary>
    [Fact]
    public void GetAllIndexSuggestions_returns_all_suggestions()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
            SuggestIndexes = true,
        });

        // Query 1 with suggestions
        interceptor.Capture(Command("SELECT * FROM Orders WHERE Status = @p0"), TimeSpan.FromMilliseconds(100));

        // Query 2 with suggestions
        interceptor.Capture(Command("SELECT * FROM Customers WHERE Region = @p0"), TimeSpan.FromMilliseconds(200));

        var suggestions = interceptor.GetAllIndexSuggestions().ToList();
        Assert.NotEmpty(suggestions);
        Assert.Equal(2, suggestions.Count);
    }

    /// <summary>
    /// Verifies that GetAllIndexSuggestions() throws ArgumentNullException for null interceptor.
    /// </summary>
    [Fact]
    public void GetAllIndexSuggestions_throws_for_null_interceptor()
    {
        var ex = Assert.Throws<ArgumentNullException>(() =>
            ((SlowQueryInterceptor)null!).GetAllIndexSuggestions());
        Assert.Equal("interceptor", ex.ParamName);
    }
}