# SlowQueryLogOptionsExtensions

Provides static factory methods and fluent configuration extensions for `SlowQueryLogOptions`, enabling developers to create preconfigured option sets and chain modifications for controlling slow query logging behavior in Entity Framework Core.

## API

### WithThresholdMilliseconds

```csharp
public static SlowQueryLogOptions WithThresholdMilliseconds(this SlowQueryLogOptions options, double milliseconds)
```

Sets the query execution time threshold in milliseconds. Queries exceeding this duration trigger logging.

- **Parameters**: `options` — the instance to configure; `milliseconds` — threshold value in milliseconds (must be non-negative).
- **Returns**: The modified `SlowQueryLogOptions` instance for fluent chaining.
- **Throws**: `ArgumentNullException` if `options` is null; `ArgumentOutOfRangeException` if `milliseconds` is negative.

### WithThresholdSeconds

```csharp
public static SlowQueryLogOptions WithThresholdSeconds(this SlowQueryLogOptions options, double seconds)
```

Sets the query execution time threshold in seconds. Internally converts to milliseconds.

- **Parameters**: `options` — the instance to configure; `seconds` — threshold value in seconds (must be non-negative).
- **Returns**: The modified `SlowQueryLogOptions` instance for fluent chaining.
- **Throws**: `ArgumentNullException` if `options` is null; `ArgumentOutOfRangeException` if `seconds` is negative.

### WithThresholdMinutes

```csharp
public static SlowQueryLogOptions WithThresholdMinutes(this SlowQueryLogOptions options, double minutes)
```

Sets the query execution time threshold in minutes. Internally converts to milliseconds.

- **Parameters**: `options` — the instance to configure; `minutes` — threshold value in minutes (must be non-negative).
- **Returns**: The modified `SlowQueryLogOptions` instance for fluent chaining.
- **Throws**: `ArgumentNullException` if `options` is null; `ArgumentOutOfRangeException` if `minutes` is negative.

### WithLogLevel

```csharp
public static SlowQueryLogOptions WithLogLevel(this SlowQueryLogOptions options, LogLevel logLevel)
```

Sets the `LogLevel` at which slow query messages are emitted.

- **Parameters**: `options` — the instance to configure; `logLevel` — the desired logging severity level.
- **Returns**: The modified `SlowQueryLogOptions` instance for fluent chaining.
- **Throws**: `ArgumentNullException` if `options` is null.

### WithParameterValues

```csharp
public static SlowQueryLogOptions WithParameterValues(this SlowQueryLogOptions options)
```

Enables inclusion of SQL parameter values in slow query log output. Useful for debugging specific query invocations.

- **Parameters**: `options` — the instance to configure.
- **Returns**: The modified `SlowQueryLogOptions` instance for fluent chaining.
- **Throws**: `ArgumentNullException` if `options` is null.

### WithoutParameterValues

```csharp
public static SlowQueryLogOptions WithoutParameterValues(this SlowQueryLogOptions options)
```

Disables inclusion of SQL parameter values in slow query log output. Use when parameter data is sensitive or verbose.

- **Parameters**: `options` — the instance to configure.
- **Returns**: The modified `SlowQueryLogOptions` instance for fluent chaining.
- **Throws**: `ArgumentNullException` if `options` is null.

### WithIndexSuggestions

```csharp
public static SlowQueryLogOptions WithIndexSuggestions(this SlowQueryLogOptions options)
```

Enables generation of index suggestions for slow queries. The analyzer inspects query patterns and proposes missing indexes.

- **Parameters**: `options` — the instance to configure.
- **Returns**: The modified `SlowQueryLogOptions` instance for fluent chaining.
- **Throws**: `ArgumentNullException` if `options` is null.

### WithoutIndexSuggestions

```csharp
public static SlowQueryLogOptions WithoutIndexSuggestions(this SlowQueryLogOptions options)
```

Disables generation of index suggestions for slow queries.

- **Parameters**: `options` — the instance to configure.
- **Returns**: The modified `SlowQueryLogOptions` instance for fluent chaining.
- **Throws**: `ArgumentNullException` if `options` is null.

### WithRankingCapacity

```csharp
public static SlowQueryLogOptions WithRankingCapacity(this SlowQueryLogOptions options, int capacity)
```

Sets the maximum number of slow queries retained in the internal ranking structure. When capacity is exceeded, the fastest entries among the tracked set are evicted.

- **Parameters**: `options` — the instance to configure; `capacity` — maximum number of tracked slow queries (must be positive).
- **Returns**: The modified `SlowQueryLogOptions` instance for fluent chaining.
- **Throws**: `ArgumentNullException` if `options` is null; `ArgumentOutOfRangeException` if `capacity` is zero or negative.

### WithOnSlowQuery

```csharp
public static SlowQueryLogOptions WithOnSlowQuery(this SlowQueryLogOptions options, Action<SlowQueryInfo> callback)
```

Registers a callback invoked whenever a slow query is detected. The callback receives a `SlowQueryInfo` object containing query details, execution time, and any index suggestions.

- **Parameters**: `options` — the instance to configure; `callback` — the action to execute on each slow query occurrence (must not be null).
- **Returns**: The modified `SlowQueryLogOptions` instance for fluent chaining.
- **Throws**: `ArgumentNullException` if `options` or `callback` is null.

### Configure

```csharp
public static SlowQueryLogOptions Configure(this SlowQueryLogOptions options, Action<SlowQueryLogOptions> configureAction)
```

Applies an arbitrary configuration action to the options instance, enabling batch modifications within a single delegate.

- **Parameters**: `options` — the instance to configure; `configureAction` — delegate receiving the options for in-place mutation (must not be null).
- **Returns**: The modified `SlowQueryLogOptions` instance for fluent chaining.
- **Throws**: `ArgumentNullException` if `options` or `configureAction` is null.

### CreateDefault

```csharp
public static SlowQueryLogOptions CreateDefault()
```

Creates a `SlowQueryLogOptions` instance with sensible defaults: threshold of 200 milliseconds, `LogLevel.Warning`, parameter values excluded, index suggestions enabled, and a ranking capacity of 100.

- **Returns**: A new `SlowQueryLogOptions` instance with default settings.

### CreateDevelopment

```csharp
public static SlowQueryLogOptions CreateDevelopment()
```

Creates a `SlowQueryLogOptions` instance tuned for development environments: threshold of 100 milliseconds, `LogLevel.Debug`, parameter values included, index suggestions enabled, and a ranking capacity of 50.

- **Returns**: A new `SlowQueryLogOptions` instance with development-oriented settings.

### CreateProduction

```csharp
public static SlowQueryLogOptions CreateProduction()
```

Creates a `SlowQueryLogOptions` instance tuned for production environments: threshold of 500 milliseconds, `LogLevel.Warning`, parameter values excluded, index suggestions enabled, and a ranking capacity of 200.

- **Returns**: A new `SlowQueryLogOptions` instance with production-oriented settings.

### CreateDebug

```csharp
public static SlowQueryLogOptions CreateDebug()
```

Creates a `SlowQueryLogOptions` instance tuned for intensive debugging: threshold of 50 milliseconds, `LogLevel.Information`, parameter values included, index suggestions enabled, and a ranking capacity of 150.

- **Returns**: A new `SlowQueryLogOptions` instance with debug-oriented settings.

### CreateCaptureAll

```csharp
public static SlowQueryLogOptions CreateCaptureAll()
```

Creates a `SlowQueryLogOptions` instance that captures every query regardless of duration: threshold of 0 milliseconds, `LogLevel.Debug`, parameter values included, index suggestions enabled, and a ranking capacity of 500.

- **Returns**: A new `SlowQueryLogOptions` instance configured to log all queries.

## Usage

### Example 1: Production setup with custom callback

```csharp
var options = SlowQueryLogOptionsExtensions.CreateProduction()
    .WithThresholdSeconds(1.5)
    .WithOnSlowQuery(info =>
    {
        Console.WriteLine($"Slow query detected: {info.ElapsedMilliseconds}ms");
        if (info.IndexSuggestions.Any())
        {
            Console.WriteLine($"Suggested indexes: {string.Join("; ", info.IndexSuggestions)}");
        }
    });

// Pass options to the interceptor or DbContext configuration
services.AddDbContext<AppDbContext>(db =>
    db.UseSlowQueryLogging(options));
```

### Example 2: Development debugging with fluent configuration

```csharp
var options = SlowQueryLogOptionsExtensions.CreateDefault()
    .Configure(opt =>
    {
        opt.WithThresholdMilliseconds(75)
           .WithLogLevel(LogLevel.Debug)
           .WithParameterValues()
           .WithRankingCapacity(30);
    })
    .WithIndexSuggestions();

// Use in a scoped interception setup
var interceptor = new SlowQueryInterceptor(options);
```

## Notes

- All `With*` and `Without*` extension methods return the same instance they receive, enabling method chaining. They mutate the existing object in-place rather than creating copies.
- Factory methods (`CreateDefault`, `CreateDevelopment`, `CreateProduction`, `CreateDebug`, `CreateCaptureAll`) each return a new, independent instance. Modifying a returned instance does not affect future calls to the same factory method.
- **Thread safety**: These methods perform no internal synchronization. Instances of `SlowQueryLogOptions` are not thread-safe for concurrent mutation. Configure options once during application startup before registering them with the interceptor. Concurrent reads from multiple threads after configuration is complete are safe provided no further writes occur.
- The `WithOnSlowQuery` callback is invoked synchronously during query interception. Avoid long-running or blocking operations inside the callback to prevent query pipeline delays.
- Setting a threshold of zero via `WithThresholdMilliseconds(0)` or `CreateCaptureAll` causes every query to be logged, which may generate substantial log volume and impact performance. Use only in diagnostic scenarios.
- `WithRankingCapacity` enforces a minimum of 1. When the internal ranking reaches capacity, the query with the shortest execution time among tracked entries is dropped to make room for a slower incoming query, preserving the set of most expensive queries.
- Index suggestions rely on the `IndexSuggestionAnalyzer` component, which performs defensive null and range checks internally. Invalid or malformed query structures are handled gracefully without throwing exceptions during analysis.
