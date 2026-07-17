# SlowQueryLogOptions

`SlowQueryLogOptions` provides configuration settings for tracking and logging inefficient database queries within an Entity Framework Core application using the `efcore-slow-query-log` library. These options define the criteria for what constitutes a "slow" query and specify how detected queries should be handled and reported.

## API

### Threshold
`public TimeSpan Threshold`
Defines the duration a query must exceed to be considered slow. Queries executing within this time limit are not recorded or analyzed.

### LogLevel
`public LogLevel LogLevel`
Specifies the logging level (e.g., `LogLevel.Warning`, `LogLevel.Information`) used when emitting logs for slow queries.

### IncludeParameterValues
`public bool IncludeParameterValues`
Determines whether the generated logs should include the specific parameter values used during query execution. Enabling this may provide better context but could potentially expose sensitive data.

### SuggestIndexes
`public bool SuggestIndexes`
Indicates whether the system should attempt to analyze the query and suggest database indexes to improve performance.

### RankingCapacity
`public int RankingCapacity`
Sets the maximum number of slow queries to track internally for ranking and analysis purposes.

### OnSlowQuery
`public Action<SlowQuerySample>? OnSlowQuery`
An optional callback delegate triggered whenever a slow query is detected. The delegate receives a `SlowQuerySample` object containing details about the slow query, allowing for custom monitoring, metrics collection, or telemetry integration.

## Usage

### Basic Configuration
```csharp
services.AddDbContext<MyDbContext>(options =>
    options.UseSqlServer(connectionString)
           .UseSlowQueryLogging(new SlowQueryLogOptions
           {
               Threshold = TimeSpan.FromMilliseconds(500),
               LogLevel = LogLevel.Warning,
               IncludeParameterValues = true,
               SuggestIndexes = true
           }));
```

### Advanced Monitoring with Callbacks
```csharp
var options = new SlowQueryLogOptions
{
    Threshold = TimeSpan.FromSeconds(1),
    RankingCapacity = 100,
    OnSlowQuery = sample => 
    {
        // Custom telemetry or alert logic
        MyMetricsService.TrackSlowQuery(sample.Query, sample.Duration);
    }
};
```

## Notes

*   **Thread-Safety:** `SlowQueryLogOptions` instances are typically intended to be configured once during application startup. While the class itself does not enforce immutability, modifications after the `DbContext` has been initialized are not guaranteed to be thread-safe or to take effect immediately.
*   **Threshold Validation:** Setting `Threshold` to `TimeSpan.Zero` or a negative value will cause all queries to be flagged as slow, which may significantly degrade application performance.
*   **RankingCapacity:** A `RankingCapacity` value of zero or less will effectively disable the internal tracking of slow queries for ranking purposes.
*   **Performance Impact:** Enabling `SuggestIndexes` requires additional analysis overhead per slow query. This should be used cautiously in high-throughput production environments.
