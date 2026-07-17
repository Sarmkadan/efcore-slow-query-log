# EfCore.SlowQueryLog

A small EF Core command interceptor that catches queries running longer than a
threshold, logs the **generated SQL**, keeps a live **ranking** of the slowest
queries, and prints **naive index suggestions** derived from the WHERE / JOIN /
ORDER BY clauses.

It is a diagnostics aid, not a query planner. The index suggestions are heuristic:
they tell you *where to look*, not *what to create*. Always confirm with the
database's own `EXPLAIN` before adding an index.

## Why

EF Core makes it very easy to write a LINQ query that generates a slow SQL
statement without noticing. `LogTo` will happily dump every command, but at 2am
you do not want to grep megabytes of SQL - you want the five slowest queries and
a hint about the missing index. That is all this does.

## Install

The library targets `net8.0` and depends only on
`Microsoft.EntityFrameworkCore.Relational`.

```bash
dotnet add package EfCore.SlowQueryLog
```

## Usage

Register it on your `DbContextOptionsBuilder`:

```csharp
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.UseSlowQueryLog(o =>
    {
        o.Threshold             = TimeSpan.FromMilliseconds(200);
        o.LogLevel              = LogLevel.Warning;
        o.SuggestIndexes        = true;
        o.IncludeParameterValues = false; // keep off in production
        o.RankingCapacity       = 25;
    }, sp.GetRequiredService<ILoggerFactory>());
});
```

A slow query then shows up in the log like this:

```
warn: EfCore.SlowQueryLog.Interception.SlowQueryInterceptor[0]
      Slow query detected: 812.4ms
      SELECT [o].[Id], [o].[Total] FROM [Orders] AS [o]
      INNER JOIN [Customers] AS [c] ON [o].[CustomerId] = [c].[Id]
      WHERE [o].[Status] = @p0 ORDER BY [o].[CreatedAt]
      Index suggestions:
        CREATE INDEX IX_Orders_Status_CustomerId_CreatedAt ON Orders (Status, CustomerId, CreatedAt);
```

### Getting the ranking programmatically

Hold a reference to the interceptor so you can read its `Ranking` later - for
example to expose a `/diagnostics/slow-queries` endpoint:

```csharp
var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
{
    Threshold = TimeSpan.FromMilliseconds(200),
});

options.UseSlowQueryLog(interceptor);

// ...later...
foreach (var q in interceptor.Ranking.Snapshot())
    Console.WriteLine($"{q.Duration.TotalMilliseconds:F0}ms  {q.Sql}");
```

### Streaming samples elsewhere

Use `OnSlowQuery` to push each sample to metrics, a message bus or a dashboard:

```csharp
o.OnSlowQuery = sample =>
    metrics.Histogram("db.slow_query_ms").Record(sample.Duration.TotalMilliseconds);
```

## SlowQueryLogOptions

`SlowQueryLogOptions` configures the behavior of the slow query interceptor. It controls
which queries are considered slow, how they are logged, whether index suggestions are
generated, and how many slow queries are kept in memory.

Example usage:

```csharp
services.AddDbContext<AppDbContext>((sp, options) =>
{
    options.UseSqlServer(connectionString);
    options.UseSlowQueryLog(o =>
    {
        o.Threshold = TimeSpan.FromMilliseconds(200);
        o.LogLevel = LogLevel.Warning;
        o.IncludeParameterValues = false; // keep off in production
        o.SuggestIndexes = true;
        o.RankingCapacity = 25;
        o.OnSlowQuery = sample => 
            Console.WriteLine($"Slow query: {sample.Duration.TotalMilliseconds:F0}ms {sample.Sql}");
    }, sp.GetRequiredService<ILoggerFactory>());
});
```

## Options

| Option | Default | Description |
|--------|---------|-------------|
| `Threshold` | `500ms` | Minimum duration for a command to count as slow. |
| `LogLevel` | `Warning` | Level used when reporting a slow query. |
| `IncludeParameterValues` | `false` | Include parameter values in the log. Off by default to avoid leaking data. |
| `SuggestIndexes` | `true` | Run the WHERE/JOIN/ORDER BY heuristic. |
| `RankingCapacity` | `25` | How many slowest queries to keep in memory. |
| `OnSlowQuery` | `null` | Callback invoked for every slow query. |

## SlowQuerySample

`SlowQuerySample` represents a captured slow query together with its execution
metadata and any index suggestions that were generated. It is a simple immutable
record that can be instantiated directly with object‑initializer syntax.

```csharp
using EfCore.SlowQueryLog;

var sample = new SlowQuerySample
{
    Sql = "SELECT * FROM Orders WHERE Id = @p0",
    Duration = TimeSpan.FromMilliseconds(850),
    CapturedAt = DateTimeOffset.UtcNow,
    Parameters = "@p0 = 42",
    Suggestions = new[]
    {
        new IndexSuggestion(
            Table: "Orders",
            Columns: new[] { "Id" },
            Reason: "filter column")
    }
};

Console.WriteLine($"Slow query ({sample.Duration.TotalMilliseconds:F0} ms): {sample.Sql}");
foreach (var suggestion in sample.Suggestions)
{
    Console.WriteLine(suggestion.ToSqlHint());
}
```

The example shows how to create a `SlowQuerySample`, optionally include the
parameter list, and iterate over any `IndexSuggestion`s to obtain the SQL hint
that can be logged or displayed.

## SlowQueryInterceptor

The `SlowQueryInterceptor` class is responsible for capturing slow queries and
providing access to the live ranking of slow queries. It can be used to capture
slow queries programmatically using the `Capture` method, and provides a
`Ranking` property to access the live ranking of slow queries. The following
example demonstrates how to use the `SlowQueryInterceptor` to capture a slow
query and access the live ranking:

```csharp
var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
{
    Threshold = TimeSpan.FromMilliseconds(200),
});

var command = new SqlCommand("SELECT * FROM Orders WHERE Id = @p0");
var duration = TimeSpan.FromMilliseconds(850);
var sample = interceptor.Capture(command, duration);

Console.WriteLine($"Slow query ({sample.Duration.TotalMilliseconds:F0} ms): {sample.Sql}");
Console.WriteLine($"Live ranking count: {interceptor.Ranking.Snapshot().Count}");
```

## How the index heuristic works

The analyzer does **not** build a real SQL parse tree. It scans the statement
with a handful of regexes:

- columns compared in a `WHERE` predicate -> candidate filter index
- columns used in a `JOIN ... ON` -> candidate join-key index (left side of
  the comparison only)
- columns in `ORDER BY` -> candidate sort index

Columns for the same table are grouped into a single composite suggestion.
Table aliases (`FROM [Orders] AS [o]`) are resolved back to the real table name.
It is intentionally conservative: if it cannot resolve a table, it drops the
column rather than guessing.

Because it is pure string work it is provider-agnostic, but the bracket style it
recognises best is the SQL Server / SQLite `[Table].[Column]` form that EF Core
emits by default.

## Limitations

- The suggestions are heuristic and can be wrong. Verify with `EXPLAIN`.
- Composite index column order follows discovery order, not selectivity.
- No de-duplication across query shapes - a parameterised query and its literal
  variant are two separate samples.

## Architecture

See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the component breakdown,
data flow through the interception pipeline, design rationale and known
limitations.

## Building and testing

```bash
dotnet build
dotnet test
```

## SlowQueryInterceptorExtensions

The `SlowQueryInterceptorExtensions` class provides a set of convenient extension methods for the `SlowQueryInterceptor` that simplify common operations such as clearing captured queries, checking if any queries have been captured, retrieving individual queries by performance characteristics, and accessing aggregated statistics about the captured slow queries.

These extensions make it easy to programmatically inspect the captured slow queries for diagnostic purposes, testing, or integration with monitoring systems.

### Usage Examples

```csharp
using EfCore.SlowQueryLog.Interception;

// Create and configure the interceptor
var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
{
    Threshold = TimeSpan.FromMilliseconds(200),
});

// Capture some slow queries...
interceptor.Capture(command1, TimeSpan.FromMilliseconds(850));
interceptor.Capture(command2, TimeSpan.FromMilliseconds(1200));

// Use extension methods to inspect captured queries
int queryCount = interceptor.GetQueryCount();
bool hasQueries = interceptor.HasCapturedQueries();

// Get all captured queries
var allQueries = interceptor.GetCapturedQueries();

// Get the slowest and fastest queries
var slowest = interceptor.GetSlowestQuery();
var fastest = interceptor.GetFastestQuery();

// Get aggregated statistics
timeSpan totalDuration = interceptor.GetTotalDuration();
double averageMs = interceptor.GetAverageDurationMs();

// Get all index suggestions across all queries
var allSuggestions = interceptor.GetAllIndexSuggestions();

// Clear captured queries when needed
interceptor.Clear();
```

## SlowQueryLogOptionsExtensions

The `SlowQueryLogOptionsExtensions` class provides a fluent interface for configuring `SlowQueryLogOptions` with method chaining. It includes extension methods for setting thresholds, log levels, parameter inclusion, index suggestions, ranking capacity, and callbacks, as well as factory methods for creating pre-configured options instances for different environments.

### Usage Examples

```csharp
using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Options;
using Microsoft.Extensions.Logging;

// Fluent configuration with method chaining
var options = SlowQueryLogOptionsExtensions.CreateDefault()
    .WithThresholdMilliseconds(300)
    .WithLogLevel(LogLevel.Warning)
    .WithParameterValues()
    .WithIndexSuggestions()
    .WithRankingCapacity(50)
    .WithOnSlowQuery(sample => 
        Console.WriteLine($"Slow query detected: {sample.Duration.TotalMilliseconds:F1}ms"));

// Alternative: using the Configure method for grouped settings
var productionOptions = SlowQueryLogOptionsExtensions.CreateProduction(thresholdMilliseconds: 500)
    .Configure(o => 
    {
        o.IncludeParameterValues = false; // Keep off in production
        o.RankingCapacity = 200;
    });

// Factory methods for common scenarios
var devOptions = SlowQueryLogOptionsExtensions.CreateDevelopment(thresholdMilliseconds: 200);
var debugOptions = SlowQueryLogOptionsExtensions.CreateDebug(thresholdMilliseconds: 100);
var captureAllOptions = SlowQueryLogOptionsExtensions.CreateCaptureAll(thresholdMilliseconds: 1);
```

## License

MIT
