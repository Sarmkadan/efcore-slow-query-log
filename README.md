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
        CREATE INDEX IX_Customers_Id ON Customers (Id);
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

## Options

| Option | Default | Description |
|--------|---------|-------------|
| `Threshold` | `500ms` | Minimum duration for a command to count as slow. |
| `LogLevel` | `Warning` | Level used when reporting a slow query. |
| `IncludeParameterValues` | `false` | Include parameter values in the log. Off by default to avoid leaking data. |
| `SuggestIndexes` | `true` | Run the WHERE/JOIN/ORDER BY heuristic. |
| `RankingCapacity` | `25` | How many slowest queries to keep in memory. |
| `OnSlowQuery` | `null` | Callback invoked for every slow query. |

## How the index heuristic works

The analyzer does **not** build a real SQL parse tree. It scans the statement
with a handful of regexes:

- columns compared in a `WHERE` predicate -> candidate filter index
- columns used in a `JOIN ... ON` -> candidate join-key index
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

## License

MIT
