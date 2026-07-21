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
		// Initialize the connection so GetEffectiveThreshold can determine the provider
		cmd.Connection = new SqliteConnection("Data Source=:memory:");
		return cmd;
	}

	/// <summary>
	/// Verifies that a query below the threshold is not recorded.
	/// </summary>
	[Fact]
	public void Query_below_threshold_not_recorded()
	{
		var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
		{
			Threshold = TimeSpan.FromMilliseconds(500),
		});

		var sample = interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(100));

		Assert.Null(sample);
		Assert.Equal(0, interceptor.Ranking.Count);
	}

	/// <summary>
	/// Verifies that a query exactly at the threshold is recorded.
	/// </summary>
	[Fact]
	public void Query_at_threshold_is_recorded()
	{
		var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
		{
			Threshold = TimeSpan.FromMilliseconds(100),
		});

		var sample = interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(100));

		Assert.NotNull(sample);
		Assert.Equal(1, interceptor.Ranking.Count);
		Assert.Equal(TimeSpan.FromMilliseconds(100), sample!.Duration);
	}

	/// <summary>
	/// Verifies that a query above the threshold is recorded.
	/// </summary>
	[Fact]
	public void Query_above_threshold_is_recorded()
	{
		var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
		{
			Threshold = TimeSpan.FromMilliseconds(100),
		});

		var sql = "SELECT [o].[Id] FROM [Orders] AS [o] WHERE [o].[Status] = @p0";
		var sample = interceptor.Capture(Command(sql), TimeSpan.FromMilliseconds(750));

		Assert.NotNull(sample);
		Assert.Equal(1, interceptor.Ranking.Count);
		Assert.Equal(TimeSpan.FromMilliseconds(750), sample!.Duration);
		Assert.Equal(sql, sample.Sql);
	}

	/// <summary>
	/// Verifies that the SQL field is populated in the captured sample.
	/// </summary>
	[Fact]
	public void Sample_sql_field_is_populated()
	{
		var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
		{
			Threshold = TimeSpan.FromMilliseconds(1),
		});

		var sql = "SELECT * FROM Customers WHERE Status = @status";
		var sample = interceptor.Capture(Command(sql), TimeSpan.FromMilliseconds(200));

		Assert.NotNull(sample);
		Assert.Equal(sql, sample!.Sql);
	}

	/// <summary>
	/// Verifies that the Duration field is populated in the captured sample.
	/// </summary>
	[Fact]
	public void Sample_duration_field_is_populated()
	{
		var duration = TimeSpan.FromMilliseconds(1234);
		var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
		{
			Threshold = TimeSpan.FromMilliseconds(1),
		});

		var sample = interceptor.Capture(Command("SELECT 1"), duration);

		Assert.NotNull(sample);
		Assert.Equal(duration, sample!.Duration);
	}

	/// <summary>
	/// Verifies that the CapturedAt field is populated in the captured sample.
	/// </summary>
	[Fact]
	public void Sample_captured_at_field_is_populated()
	{
		var before = DateTimeOffset.UtcNow.AddSeconds(-1);
		var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
		{
			Threshold = TimeSpan.FromMilliseconds(1),
		});

		var sample = interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(200));

		Assert.NotNull(sample);
		Assert.InRange(sample!.CapturedAt, before, DateTimeOffset.UtcNow.AddSeconds(1));
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
			RedactParameters = false,
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
	/// Verifies that parameters are redacted when RedactParameters is enabled.
	/// </summary>
	[Fact]
	public void RedactParameters_works()
	{
		var redacted = new SlowQueryInterceptor(new SlowQueryLogOptions
		{
			Threshold = TimeSpan.FromMilliseconds(1),
			IncludeParameterValues = true,
			RedactParameters = true,
		});
		var sample1 = redacted.Capture(Command("SELECT @p0", ("@p0", 42)), TimeSpan.FromMilliseconds(50));
		Assert.NotNull(sample1!.Parameters);
		Assert.Contains("=?", sample1.Parameters);
		Assert.DoesNotContain("42", sample1.Parameters);

		var notRedacted = new SlowQueryInterceptor(new SlowQueryLogOptions
		{
			Threshold = TimeSpan.FromMilliseconds(1),
			IncludeParameterValues = true,
			RedactParameters = false,
		});
		var sample2 = notRedacted.Capture(Command("SELECT @p0", ("@p0", 42)), TimeSpan.FromMilliseconds(50));
		Assert.NotNull(sample2!.Parameters);
		Assert.Contains("42", sample2.Parameters);
		Assert.DoesNotContain("=?", sample2.Parameters);
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

	/// <summary>
	/// Verifies that exceptions in the OnSlowQuery callback do not break query execution.
	/// </summary>
	[Fact]
	public void OnSlowQuery_callback_exception_does_not_break_execution()
	{
		var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
		{
			Threshold = TimeSpan.FromMilliseconds(1),
			OnSlowQuery = s => throw new InvalidOperationException("Callback failed")
		});

		// This should not throw even though the callback throws
		var sample = interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(200));
		Assert.NotNull(sample);
	}

	/// <summary>
	/// Verifies that provider-specific thresholds override the default threshold.
	/// </summary>
	[Fact]
	public void Provider_specific_threshold_overrides_default()
	{
		var options = new SlowQueryLogOptions
		{
			Threshold = TimeSpan.FromMilliseconds(100),
			ProviderThresholds = new Dictionary<string, TimeSpan> { ["SqliteConnection"] = TimeSpan.FromMilliseconds(500) }
		};
		var interceptor = new SlowQueryInterceptor(options);

		// Should use provider-specific threshold (500ms)
		var sample = interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(200));
		Assert.Null(sample);

		// Should now be recorded with provider-specific threshold
		sample = interceptor.Capture(Command("SELECT 1"), TimeSpan.FromMilliseconds(600));
		Assert.NotNull(sample);
	}

	/// <summary>
	/// Verifies that multiple slow queries are ranked correctly by duration.
	/// </summary>
	[Fact]
	public void Multiple_queries_ranked_by_duration()
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

		var snapshot = interceptor.Ranking.Snapshot();
		Assert.Equal(5, snapshot.Count);
		Assert.Equal(TimeSpan.FromMilliseconds(1500), snapshot[0].Duration);
		Assert.Equal(TimeSpan.FromMilliseconds(1000), snapshot[1].Duration);
		Assert.Equal(TimeSpan.FromMilliseconds(500), snapshot[2].Duration);
		Assert.Equal(TimeSpan.FromMilliseconds(300), snapshot[3].Duration);
		Assert.Equal(TimeSpan.FromMilliseconds(200), snapshot[4].Duration);
	}

	/// <summary>
	/// Verifies that the ranking capacity limits the number of stored samples.
	/// </summary>
	[Fact]
	public void Ranking_capacity_limits_samples()
	{
		var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
		{
			Threshold = TimeSpan.FromMilliseconds(1),
			RankingCapacity = 3,
		});

		// Add more queries than the capacity
		for (var i = 0; i < 10; i++)
		{
			interceptor.Capture(Command($"SELECT {i}"), TimeSpan.FromMilliseconds(100 + i * 10));
		}

		Assert.Equal(3, interceptor.Ranking.Count);
	}
}
