# SlowQueryInterceptor

The `SlowQueryInterceptor` is an Entity Framework Core command interceptor designed to identify, rank, and capture diagnostic samples of database queries that exceed specified performance thresholds. By hooking into the execution pipeline of `DbCommand` operations, this interceptor tracks query duration for synchronous and asynchronous reader, non-query, and scalar operations, providing developers with actionable insights into performance bottlenecks within their data access layer.

## API

### Constructors

#### `public SlowQueryInterceptor()`
Initializes a new instance of the `SlowQueryInterceptor`.

### Properties

#### `public SlowQueryRanking Ranking`
Gets or sets the `SlowQueryRanking` instance used to accumulate and rank statistics for queries executed through this interceptor.

#### `public SlowQuerySample? Capture`
Gets or sets the `SlowQuerySample` configuration or instance used to capture detailed diagnostic information for detected slow queries.

### Methods

#### `public override DbDataReader ReaderExecuted(DbCommand command, CommandExecutedEventData eventData, DbDataReader result)`
Intersects the synchronous execution of a command that returns a `DbDataReader`. Tracks the execution duration and records statistics in the associated `Ranking` and `Capture` instances.

#### `public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken ct = default)`
Intersects the asynchronous execution of a command that returns a `DbDataReader`. Tracks the execution duration asynchronously and records statistics.

#### `public override int NonQueryExecuted(DbCommand command, CommandExecutedEventData eventData, int result)`
Intersects the synchronous execution of a non-query command (e.g., INSERT, UPDATE, DELETE). Tracks the execution duration and records statistics.

#### `public override async ValueTask<int> NonQueryExecutedAsync(DbCommand command, CommandExecutedEventData eventData, int result, CancellationToken ct = default)`
Intersects the asynchronous execution of a non-query command. Tracks the execution duration asynchronously and records statistics.

#### `public override object? ScalarExecuted(DbCommand command, CommandExecutedEventData eventData, object? result)`
Intersects the synchronous execution of a command returning a scalar value. Tracks the execution duration and records statistics.

#### `public override async ValueTask<object?> ScalarExecutedAsync(DbCommand command, CommandExecutedEventData eventData, object? result, CancellationToken ct = default)`
Intersects the asynchronous execution of a command returning a scalar value. Tracks the execution duration asynchronously and records statistics.

## Usage

### Registering the Interceptor in `DbContext`

```csharp
protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
{
    var slowQueryInterceptor = new SlowQueryInterceptor
    {
        Ranking = new SlowQueryRanking(threshold: TimeSpan.FromMilliseconds(500)),
        Capture = new SlowQuerySample()
    };

    optionsBuilder.AddInterceptors(slowQueryInterceptor);
}
```

### Accessing Collected Data

```csharp
public void LogSlowQueries(SlowQueryInterceptor interceptor)
{
    var topQueries = interceptor.Ranking.GetTopSlowQueries();
    foreach (var query in topQueries)
    {
        Console.WriteLine($"Query: {query.CommandText}, Duration: {query.Duration}");
    }
}
```

## Notes

*   **Thread Safety:** While `SlowQueryInterceptor` is designed to be added to `DbContext` options, ensure that the associated `Ranking` and `Capture` objects are thread-safe if the `DbContext` is shared or if the interceptor is registered as a singleton across multiple scopes.
*   **Performance Overhead:** Capturing detailed query samples via the `Capture` property introduces overhead. It is recommended to use appropriate filtering or sampling rates in production environments to minimize impact on database throughput.
*   **Exceptions:** These methods are overrides of standard EF Core interceptor methods; they generally do not throw exceptions themselves but will propagate exceptions thrown by the underlying command execution if not handled by the EF Core interceptor pipeline.
