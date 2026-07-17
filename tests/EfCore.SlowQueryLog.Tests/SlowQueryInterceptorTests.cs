using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Microsoft.Data.Sqlite;
using Xunit;

/// <summary>
/// Tests for the SlowQueryInterceptor class.
/// </summary>
public class SlowQueryInterceptorTests
{
    /// <summary>
    /// Creates a new SqliteCommand with the given SQL and parameters.
    /// </summary>
    /// <param name="sql">The SQL to execute.</param>
    /// <param name="ps">The parameters to add to the command.</param>
    /// <returns>A new SqliteCommand instance.</returns>
    private static SqliteCommand Command(string sql, params (string name, object value)[] ps)
    {
        var cmd = new SqliteCommand { CommandText = sql };
        foreach (var (name, value) in ps)
            cmd.Parameters.AddWithValue(name, value);
        return cmd;
    }

    /// <summary>
    /// Verifies that a fast query is ignored by the SlowQueryInterceptor.
    /// </summary>
    [Fact]
    public void Fast_query_is_ignored()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(500),
        });

        var sample = interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(10));

        Assert.Null(sample);
        Assert.Equal(0, interceptor.Ranking.Count);
    }

    /// <summary>
    /// Verifies that a slow query is captured and ranked by the SlowQueryInterceptor.
    /// </summary>
    [Fact]
    public void Slow_query_is_captured_and_ranked()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(100),
        });

        var sql = "SELECT [o].[Id] FROM [Orders] AS [o] WHERE [o].[Status] = @p0";
        var sample = interceptor.Capture(Command(sql), TimeSpan.FromMilliseconds(750));

        Assert.NotNull(sample);
        Assert.Equal(1, interceptor.Ranking.Count);
        Assert.Contains(sample!.Suggestions, s => s.Table == "Orders" && s.Columns.Contains("Status"));
    }

    /// <summary>
    /// Verifies that parameters are captured only when IncludeParameterValues is enabled.
    /// </summary>
    [Fact]
    public void Parameters_captured_only_when_enabled()
    {
        var withParams = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
            IncludeParameterValues = true,
        });
        var sample = withParams.Capture(Command("SELECT @p0", ("@p0", 42)), TimeSpan.FromMilliseconds(50));
        Assert.NotNull(sample!.Parameters);
        Assert.Contains("42", sample.Parameters);

        var noParams = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
            IncludeParameterValues = false,
        });
        var sample2 = noParams.Capture(Command("SELECT @p0", ("@p0", 42)), TimeSpan.FromMilliseconds(50));
        Assert.Null(sample2!.Parameters);
    }

    /// <summary>
    /// Verifies that the OnSlowQuery callback is invoked by the SlowQueryInterceptor.
    /// </summary>
    [Fact]
    public void OnSlowQuery_callback_is_invoked()
    {
        SlowQuerySample? seen = null;
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
            OnSlowQuery = s => seen = s,
        });

        interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(200));

        Assert.NotNull(seen);
    }

    /// <summary>
    /// Verifies that suggestions are disabled when SuggestIndexes is set to false.
    /// </summary>
    [Fact]
    public void Suggestions_disabled_produces_none()
    {
        var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1),
            SuggestIndexes = false,
        });

        var sql = "SELECT * FROM [Orders] AS [o] WHERE [o].[Status] = @p0";
        var sample = interceptor.Capture(Command(sql), TimeSpan.FromMilliseconds(50));

        Assert.Empty(sample!.Suggestions);
    }

    /// <summary>
    /// Verifies that an invalid threshold throws an ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void Invalid_threshold_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SlowQueryInterceptor(new SlowQueryLogOptions { Threshold = TimeSpan.Zero }));
    }
}
