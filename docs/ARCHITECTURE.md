# Architecture

## Overview

EfCore.SlowQueryLog is a single-assembly library (`net8.0`, depends only on
`Microsoft.EntityFrameworkCore.Relational`) that plugs into the EF Core
interception pipeline. It measures every executed command, and when a command's
duration crosses a configurable threshold it:

1. captures the generated SQL (and optionally parameter values) as a `SlowQuerySample`,
2. inserts the sample into a bounded in-memory ranking of the slowest queries,
3. writes a formatted report to an `ILogger`,
4. optionally runs a regex-based heuristic that proposes candidate indexes,
5. invokes a user callback (`OnSlowQuery`) for external sinks.

There is no background thread, no storage, no network. Everything happens
synchronously inside the interceptor callback on the thread that executed the
command.

## Component breakdown

| Component | File | Responsibility |
|-----------|------|----------------|
| `SlowQueryInterceptor` | `src/EfCore.SlowQueryLog/Interception/SlowQueryInterceptor.cs` | The core. Derives from `DbCommandInterceptor` and overrides all six *Executed hooks (`ReaderExecuted[Async]`, `NonQueryExecuted[Async]`, `ScalarExecuted[Async]`). Each hook funnels into `Capture(command, duration)`, which does the threshold check, sample construction, ranking insert, logging and callback dispatch. `Capture` is public so tests can drive it without a database. |
| `SlowQueryLogOptions` | `src/EfCore.SlowQueryLog/Options/SlowQueryLogOptions.cs` | Plain options object: `Threshold` (default 500 ms), `LogLevel` (default Warning), `IncludeParameterValues` (default off), `SuggestIndexes` (default on), `RankingCapacity` (default 25), `OnSlowQuery` callback. `Validate()` rejects non-positive threshold/capacity and is called from both the interceptor constructor and the registration extension. |
| `SlowQueryRanking` | `src/EfCore.SlowQueryLog/Reporting/SlowQueryRanking.cs` | Thread-safe bounded list of the slowest samples, ordered by duration descending. `Add` locks, appends, re-sorts, and trims to capacity. `Snapshot()` returns a copied array under the lock. |
| `IndexSuggestionAnalyzer` | `src/EfCore.SlowQueryLog/Analysis/IndexSuggestionAnalyzer.cs` | Regex-based scan of the SQL text. Extracts columns from `WHERE` predicates, `JOIN ... ON` conditions and `ORDER BY` terms; resolves aliases via a `FROM`/`JOIN` alias map; groups columns per table into one composite `IndexSuggestion` per table. Not a SQL parser - see limitations. |
| `SlowQuerySample` / `IndexSuggestion` | `src/EfCore.SlowQueryLog/SlowQuerySample.cs` | Immutable records. `SlowQuerySample` carries SQL, duration, capture time, optional formatted parameters, and suggestions. `IndexSuggestion.ToSqlHint()` renders a `CREATE INDEX ...` statement. |
| `SlowQueryLogExtensions` | `src/EfCore.SlowQueryLog/SlowQueryLogExtensions.cs` | Two `UseSlowQueryLog` overloads on `DbContextOptionsBuilder`: one builds a fresh interceptor from an options delegate (+ optional `ILoggerFactory`), the other registers a pre-built interceptor instance so the caller keeps a handle to its `Ranking`. |

## Data flow

```
EF Core executes a DbCommand
  -> SlowQueryInterceptor.*Executed hook fires with CommandExecutedEventData
     -> Capture(command, eventData.Duration)
        -> duration < Threshold?  yes: return null (no allocation beyond the check)
        -> SuggestIndexes? run IndexSuggestionAnalyzer.Analyze(commandText)
        -> build SlowQuerySample
        -> Ranking.Add(sample)          (lock, insert, sort, trim)
        -> Report(sample) via ILogger   (skipped if level disabled)
        -> Options.OnSlowQuery?(sample)
```

The interceptor uses only the post-execution (`*Executed`) hooks; timing comes
from EF Core's own `eventData.Duration`, so the library never starts its own
stopwatch and adds no overhead to the pre-execution path.

## Key design decisions

- **Post-execution hooks only, EF-provided duration.** Simpler and cheaper than
  wrapping execution; trade-off: commands that throw never reach the
  `*Executed` hooks, so a query that times out with an exception is not
  captured (see limitations).
- **Fast-path threshold check first.** Below-threshold commands cost one
  comparison; nothing is allocated.
- **Ranking is per-interceptor instance, in-memory.** No global static state,
  so two contexts with separate interceptors keep separate rankings. The
  second `UseSlowQueryLog` overload exists precisely so an app can hold the
  instance and expose `Ranking.Snapshot()` from a diagnostics endpoint. The
  `Add` path is O(n log n) in capacity, which is fine because capacity is
  small (default 25).
- **Regex heuristic instead of a SQL parser.** Provider-agnostic, zero extra
  dependencies, and honest about its output ("hint, not guarantee"). It is
  conservative: an unresolvable table alias drops the column instead of
  guessing. Trade-off: it misses subqueries, functions over columns, quoted
  identifiers other than `[...]`, and it treats both sides of a join
  equality as index candidates.
- **Parameter values off by default.** `IncludeParameterValues = false`
  prevents accidental PII/secret leakage into logs; the sample stores a
  pre-formatted string rather than the live `DbParameterCollection`.
- **NullLogger fallback.** Constructing the interceptor without a logger is
  valid; ranking and the `OnSlowQuery` callback still work, only the log
  report is dropped.

## Extension points

- `SlowQueryLogOptions.OnSlowQuery` - per-sample callback for metrics,
  dashboards, message buses.
- Constructor injection of a shared `SlowQueryRanking` - several interceptors
  (e.g. across multiple DbContexts) can aggregate into one ranking.
- `UseSlowQueryLog(interceptor)` overload - bring your own instance, keep the
  reference for reporting.
- `Capture()` being public - hosts and tests can feed synthetic commands and
  durations directly.

## Known limitations

- **Failed commands are invisible.** Only `*Executed` hooks are overridden;
  `CommandFailed`/`CommandFailedAsync` are not, so queries that error out
  (including timeouts) are never ranked or logged by this library.
- Index suggestions are heuristic string work; composite column order follows
  discovery order, not selectivity. Verify with `EXPLAIN`.
- No de-duplication across query shapes: a parameterised statement and a
  literal variant of it are two separate ranking entries.
- The alias map requires an alias (or a following keyword) after the table
  name to match; a bare `FROM Orders` at the very end of a statement may not
  register the table, in which case unqualified columns are dropped.
- The ranking never expires entries; a one-off cold-start query can occupy a
  slot until `Clear()` is called or the process restarts.
