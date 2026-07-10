using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Microsoft.Data.Sqlite;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

public class SlowQueryInterceptorTests
{
    private static SqliteCommand Command(string sql, params (string name, object value)[] ps)
    {
        var cmd = new SqliteCommand { CommandText = sql };
        foreach (var (name, value) in ps)
            cmd.Parameters.AddWithValue(name, value);
        return cmd;
    }

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

    [Fact]
    public void Invalid_threshold_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            new SlowQueryInterceptor(new SlowQueryLogOptions { Threshold = TimeSpan.Zero }));
    }
}
