# EfCore.SlowQueryLog

// # EfCore.SlowQueryLog
//
// A small EF Core command interceptor that catches queries running longer than a
// threshold, logs the **generated SQL**, keeps a live **ranking** of the slowest
// queries, and prints **naive index suggestions** derived from the WHERE / JOIN /
// ORDER BY clauses.
//
// It is a diagnostics aid, not a query planner. The index suggestions are heuristic:
// they tell you *where to look*, not *what to create*. Always confirm with the
// database's own `EXPLAIN` before adding an index.
//
// ## Why
//
// EF Core makes it very easy to write a LINQ query that generates a slow SQL
// statement without noticing. `LogTo` will happily dump every command, but at
// 2am you do not want to grep megabytes of SQL - you want the five slowest
// queries and a hint about the missing index. That is all this does.
//
// ## Install
//
// The library targets `net8.0` and depends only on
// `Microsoft.EntityFrameworkCore.Relational`.
//
// ```bash
// dotnet add package EfCore.SlowQueryLog
// ```
//
// ## Usage
//
// Register it on your `DbContextOptionsBuilder`:
//
// ```csharp
// services.AddDbContext<AppDbContext>((sp, options) =>
// {
//     options.UseSqlServer(connectionString);
//     options.UseSlowQueryLog(o =>
//     {
//         o.Threshold             = TimeSpan.FromMilliseconds(200);
//         o.LogLevel              = LogLevel.Warning;
//         o.SuggestIndexes        = true;
//         o.IncludeParameterValues = false; // keep off in production
//         o.RankingCapacity       = 25;
//     }, sp.GetRequiredService<ILoggerFactory>());
// });
// ```
//
// A slow query then shows up in the log like this:
//
// ```
// warn: EfCore.SlowQueryLog.Interception.SlowQueryInterceptor[0]
//       Slow query detected: 812.4ms
//       SELECT [o].[Id], [o].[Total] FROM [Orders] AS [o]
//       INNER JOIN [Customers] AS [c] ON [o].[CustomerId] = [c].[Id]
//       WHERE [o].[Status] = @p0 ORDER BY [o].[CreatedAt]
//       Index suggestions:
//         CREATE INDEX IX_Orders_Status_CustomerId_CreatedAt ON Orders (Status, CustomerId, CreatedAt);
// ```
//
// ### Getting the ranking programmatically
//
// Hold a reference to the interceptor so you can read its `Ranking` later - for
// example to expose a `/diagnostics/slow-queries` endpoint:
//
// ```csharp
// var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
// {
//     Threshold = TimeSpan.FromMilliseconds(200),
// });
//
// options.UseSlowQueryLog(interceptor);
//
// // ...later...
// foreach (var q in interceptor.Ranking.Snapshot())
//     Console.WriteLine($"{q.Duration.TotalMilliseconds:F0}ms  {q.Sql}");
// ```
//
// ### Streaming samples elsewhere
//
// Use `OnSlowQuery` to push each sample to metrics, a message bus or a dashboard:
//
// ```csharp
// o.OnSlowQuery = sample =>
//     metrics.Histogram("db.slow_query_ms").Record(sample.Duration.TotalMilliseconds);
// ```
//
// ## SlowQueryLogOptions
//
// `SlowQueryLogOptions` configures the behavior of the slow query interceptor. It controls
// which queries are considered slow, how they are logged, whether index suggestions are
// generated, and how many slow queries are kept in memory.
//
// Example usage:
//
// ```csharp
// services.AddDbContext<AppDbContext>((sp, options) =>
// {
//     options.UseSqlServer(connectionString);
//     options.UseSlowQueryLog(o =>
//     {
//         o.Threshold = TimeSpan.FromMilliseconds(200);
//         o.LogLevel = LogLevel.Warning;
//         o.IncludeParameterValues = false; // keep off in production
//         o.SuggestIndexes = true;
//         o.RankingCapacity = 25;
//         o.OnSlowQuery = sample => 
//             Console.WriteLine($"Slow query: {sample.Duration.TotalMilliseconds:F0}ms {sample.Sql}");
//     }, sp.GetRequiredService<ILoggerFactory>());
// });
// ```
//
// ## Options
//
// | Option | Default | Description |
// |--------|---------|-------------|
// | `Threshold` | `500ms` | Minimum duration for a command to count as slow. |
// | `LogLevel` | `Warning` | Level used when reporting a slow query. |
// | `IncludeParameterValues` | `false` | Include parameter values in the log. Off by default to avoid leaking data. |
// | `SuggestIndexes` | `true` | Run the WHERE/JOIN/ORDER BY heuristic. |
// | `RankingCapacity` | `25` | How many slowest queries to keep in memory. |
// | `OnSlowQuery` | `null` | Callback invoked for every slow query. |
//
// ## SlowQuerySample
//
// `SlowQuerySample` represents a captured slow query together with its execution
// metadata and any index suggestions that were generated. It is a simple immutable
// record that can be instantiated directly with object‑initializer syntax.
//
// ```csharp
// using EfCore.SlowQueryLog;
//
// var sample = new SlowQuerySample
// {
//     Sql = "SELECT * FROM Orders WHERE Id = @p0",
//     Duration = TimeSpan.FromMilliseconds(850),
//     CapturedAt = DateTimeOffset.UtcNow,
//     Parameters = "@p0 = 42",
//     Suggestions = new[]
//     {
//         new IndexSuggestion(
//             Table: "Orders",
//             Columns: new[] { "Id" },
//             Reason: "filter column")
//     }
// };
//
// Console.WriteLine($"Slow query ({sample.Duration.TotalMilliseconds:F0} ms): {sample.Sql}");
// foreach (var suggestion in sample.Suggestions)
// {
//     Console.WriteLine(suggestion.ToSqlHint());
// }
// ```
//
// The example shows how to create a `SlowQuerySample`, optionally include the
// parameter list, and iterate over any `IndexSuggestion`s to obtain the SQL hint
// that can be logged or displayed.
//
// ## SlowQueryInterceptor
//
// The `SlowQueryInterceptor` class is responsible for capturing slow queries and
// providing access to the live ranking of slow queries. It can be used to capture
// slow queries programmatically using the `Capture` method, and provides a
// `Ranking` property to access the live ranking of slow queries. The following
// example demonstrates how to use the `SlowQueryInterceptor` to capture a slow
// query and access the live ranking:
//
// ```csharp
// var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
// {
//     Threshold = TimeSpan.FromMilliseconds(200),
// });
//
// var command = new SqlCommand("SELECT * FROM Orders WHERE Id = @p0");
// var duration = TimeSpan.FromMilliseconds(850);
// var sample = interceptor.Capture(command, duration);
//
// Console.WriteLine($"Slow query ({sample.Duration.TotalMilliseconds:F0} ms): {sample.Sql}");
// Console.WriteLine($"Live ranking count: {interceptor.Ranking.Snapshot().Count}");
// ```
//
// ## How the index heuristic works
//
// The analyzer does **not** build a real SQL parse tree. It scans the statement
// with a handful of regexes:
//
// - columns compared in a `WHERE` predicate -> candidate filter index
// - columns used in a `JOIN ... ON` -> candidate join-key index (left side of
//   the comparison only)
// - columns in `ORDER BY` -> candidate sort index
//
// Columns for the same table are grouped into a single composite suggestion.
// Table aliases (`FROM [Orders] AS [o]`) are resolved back to the real table name.
// It is intentionally conservative: if it cannot resolve a table, it drops the
// column rather than guessing.
//
// Because it is pure string work it is provider-agnostic, but the bracket style it
// recognises best is the SQL Server / SQLite `[Table].[Column]` form that EF Core
// emits by default.
//
// ## Limitations
//
// - The suggestions are heuristic and can be wrong. Verify with `EXPLAIN`.
// - Composite index column order follows discovery order, not selectivity.
// - No de-duplication across query shapes - a parameterised query and its literal
//   variant are two separate samples.
//
// ## Architecture
//
// See [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) for the component breakdown,
// data flow through the interception pipeline, design rationale and known
// limitations.
//
// ## Building and testing
//
// ```bash
// dotnet build
// dotnet test
// ```
//
// ## SlowQueryInterceptorExtensions
//
// The `SlowQueryInterceptorExtensions` class provides a set of convenient extension methods for the `SlowQueryInterceptor` that simplify common operations such as clearing captured queries, checking if any queries have been captured, retrieving individual queries by performance characteristics, and accessing aggregated statistics about the captured slow queries.
//
// These extensions make it easy to programmatically inspect the captured slow queries for diagnostic purposes, testing, or integration with monitoring systems.
//
// ### Usage Examples
//
// ```csharp
// using EfCore.SlowQueryLog.Interception;
//
// // Create and configure the interceptor
// var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
// {
//     Threshold = TimeSpan.FromMilliseconds(200),
// });
//
// // Capture some slow queries...
// interceptor.Capture(command1, TimeSpan.FromMilliseconds(850));
// interceptor.Capture(command2, TimeSpan.FromMilliseconds(1200));
//
// // Use extension methods to inspect captured queries
// int queryCount = interceptor.GetQueryCount();
// bool hasQueries = interceptor.HasCapturedQueries();
//
// // Get all captured queries
// var allQueries = interceptor.GetCapturedQueries();
//
// // Get the slowest and fastest queries
// var slowest = interceptor.GetSlowestQuery();
// var fastest = interceptor.GetFastestQuery();
//
// // Get aggregated statistics
// timeSpan totalDuration = interceptor.GetTotalDuration();
// double averageMs = interceptor.GetAverageDurationMs();
//
// // Get all index suggestions across all queries
// var allSuggestions = interceptor.GetAllIndexSuggestions();
//
// // Clear captured queries when needed
// interceptor.Clear();
// ```
//
// ## SlowQueryInterceptorTestsExtensions
//
// The `SlowQueryInterceptorTestsExtensions` class offers a collection of helper
// extension methods that make writing unit tests for `SlowQueryInterceptor` easier.
// It includes methods for creating test commands, configuring interceptors with
// common test settings, capturing a query, and retrieving captured samples and
// counts.
//
// ### Usage Example
//
// ```csharp
// using EfCore.SlowQueryLog.Interception;
// using EfCore.SlowQueryLog.Tests;
// using Microsoft.Data.Sqlite;
//
// // Create a test interceptor with a 200 ms threshold
// var interceptor = TimeSpan.FromMilliseconds(200).CreateTestInterceptor(
//     includeParameters: true,
//     suggestIndexes: false);
//
// // Capture a slow query using the test helper
// var sample = interceptor.CaptureSlowQuery(
//     \"SELECT * FROM Orders WHERE Id = @p0\",
//     TimeSpan.FromMilliseconds(850),
//     (\"@p0\", 42));
//
// // Inspect the captured sample (if any)
// if (sample != null)
// {
//     Console.WriteLine($\"Captured duration: {sample.Duration.TotalMilliseconds} ms\");
//     Console.WriteLine($\"SQL: {sample.Sql}\");
// }
//
// // Retrieve all captured samples and the total count
// var allSamples = interceptor.GetCapturedSamples();
// var count = interceptor.GetSlowQueryCount();
//
// Console.WriteLine($\"Total captured slow queries: {count}\");
// ```
//
// ## SlowQueryLogOptionsExtensions
//
// The `SlowQueryLogOptionsExtensions` class provides a fluent interface for configuring `SlowQueryLogOptions` with method chaining. It includes extension methods for setting thresholds, log levels, parameter inclusion, index suggestions, ranking capacity, and callbacks, as well as factory methods for creating pre-configured options instances for different environments.
//
// ### Usage Examples
//
// ```csharp
// using EfCore.SlowQueryLog;
// using EfCore.SlowQueryLog.Options;
// using Microsoft.Extensions.Logging;
//
// // Fluent configuration with method chaining
// var options = SlowQueryLogOptionsExtensions.CreateDefault()
//     .WithThresholdMilliseconds(300)
//     .WithLogLevel(LogLevel.Warning)
//     .WithParameterValues()
//     .WithIndexSuggestions()
//     .WithRankingCapacity(50)
//     .WithOnSlowQuery(sample => 
//         Console.WriteLine($"Slow query detected: {sample.Duration.TotalMilliseconds:F1}ms"));
//
// // Alternative: using the Configure method for grouped settings
// var productionOptions = SlowQueryLogOptionsExtensions.CreateProduction(thresholdMilliseconds: 500)
//     .Configure(o => 
//     {
//         o.IncludeParameterValues = false; // Keep off in production
//         o.RankingCapacity = 200;
//     });
//
// // Factory methods for common scenarios
// var devOptions = SlowQueryLogOptionsExtensions.CreateDevelopment(thresholdMilliseconds: 200);
// var debugOptions = SlowQueryLogOptionsExtensions.CreateDebug(thresholdMilliseconds: 100);
// var captureAllOptions = SlowQueryLogOptionsExtensions.CreateCaptureAll(thresholdMilliseconds: 1);
// ```
//
// ## SlowQueryInterceptorTests
//
// `SlowQueryInterceptorTests` is a unit test class that verifies the behavior of the `SlowQueryInterceptor` class. It tests various scenarios including fast queries that should be ignored, slow queries that should be captured and ranked, parameter capturing, callback invocation, and configuration options like index suggestions and threshold validation.
//
// The test class demonstrates how to use the interceptor in isolation with synthetic commands and timing, making it useful for understanding the expected behavior of the interceptor without requiring a full database setup.
//
// ### Usage Example
//
// ```csharp
// using EfCore.SlowQueryLog;
// using EfCore.SlowQueryLog.Interception;
// using Microsoft.Data.Sqlite;
//
// // Create an interceptor with a 500ms threshold
// var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions
// {
//     Threshold = TimeSpan.FromMilliseconds(500),
//     IncludeParameterValues = true,
//     SuggestIndexes = true
// });
//
// // Capture a fast query (should return null)
// var fastCommand = new SqliteCommand { CommandText = \"SELECT 1\" };
// var fastSample = interceptor.Capture(fastCommand, TimeSpan.FromMilliseconds(10));
// Console.WriteLine(fastSample); // null
//
// // Capture a slow query (should return a SlowQuerySample)
// var slowCommand = new SqliteCommand { CommandText = \"SELECT * FROM Orders WHERE Status = @p0\" };
// slowCommand.Parameters.AddWithValue(\"@p0\", \"active\");
// var slowSample = interceptor.Capture(slowCommand, TimeSpan.FromMilliseconds(850));
//
// if (slowSample != null)
// {
//     Console.WriteLine($\"Captured: {slowSample.Duration.TotalMilliseconds}ms\");
//     Console.WriteLine($\"Parameters: {slowSample.Parameters}\");
//     Console.WriteLine($\"Suggestions: {slowSample.Suggestions.Count}\");
//     foreach (var suggestion in slowSample.Suggestions)
//     {
//         Console.WriteLine($\"  {suggestion.Table}: {string.Join(\", \", suggestion.Columns)}\");
//     }
// }
//
// // Access the live ranking
// Console.WriteLine($\"Ranking count: {interceptor.Ranking.Count}\");
// ```
//
// ## IndexSuggestionAnalyzerTests
//
// `IndexSuggestionAnalyzerTests` is a unit test class that verifies the index suggestion generation logic in the `IndexSuggestionAnalyzer` class. It tests how the analyzer extracts index suggestions from SQL queries by examining WHERE clauses, JOIN conditions, and ORDER BY clauses, while correctly ignoring parameter markers, numeric literals, and ordinal values in ORDER BY clauses.
//
// ### Usage Example
//
// ```csharp
// using EfCore.SlowQueryLog.Analysis;
// using Xunit;
//
// // Create an instance of the analyzer
// var analyzer = new IndexSuggestionAnalyzer();
//
// // Test a simple WHERE clause
// var sql = \"SELECT [c].[Id], [c].[Email] FROM [Customers] AS [c] WHERE [c].[Email] = @p0\";
// var suggestions = analyzer.Analyze(sql);
//
// Assert.Single(suggestions);
// Assert.Equal(\"Customers\", suggestions[0].Table);
// Assert.Contains(\"Email\", suggestions[0].Columns);
//
// // Test a query with JOIN and ORDER BY
// var complexSql = @\"SELECT [o].[Id] FROM [Orders] AS [o]
//                     INNER JOIN [Customers] AS [c] ON [o].[CustomerId] = [c].[Id]
//                     WHERE [o].[Status] = @p0
//                     ORDER BY [o].[CreatedAt]\";
//
// var complexSuggestions = analyzer.Analyze(complexSql);
// var ordersSuggestion = Assert.Single(complexSuggestions, x => x.Table == \"Orders\");
// Assert.Contains(\"CustomerId\", ordersSuggestion.Columns);
// Assert.Contains(\"Status\", ordersSuggestion.Columns);
// Assert.Contains(\"CreatedAt\", ordersSuggestion.Columns);
//
// // Test that ToSqlHint generates proper CREATE INDEX statement
// var suggestion = new IndexSuggestion(\"Orders\", new[] { \"CustomerId\", \"Status\" }, \"test\");
// Assert.Equal(\"CREATE INDEX IX_Orders_CustomerId_Status ON Orders (CustomerId, Status);\", suggestion.ToSqlHint());
// ```
//
// ## EndToEndInterceptionTestsExtensions
//
// Extension methods for `EndToEndInterceptionTests` that provide utility functionality for end-to-end testing of EF Core slow query interception scenarios. These methods simplify the creation of test infrastructure, including in-memory database connections, slow query interceptors with configurable thresholds, and utilities for inspecting captured slow queries.
//
// ### Usage Examples
//
// ```csharp
// using EfCore.SlowQueryLog;
// using EfCore.SlowQueryLog.Interception;
// using EfCore.SlowQueryLog.Tests;
// using Microsoft.Data.Sqlite;
//
// // Create a test instance
// var test = new EndToEndInterceptionTests();
//
// // Create an in-memory SQLite connection for testing
// using var connection = test.CreateInMemoryConnection();
//
// // Create a slow query interceptor with a custom threshold
// var interceptor = test.CreateSlowQueryInterceptor(TimeSpan.FromMilliseconds(300));
//
// // Create a slow query interceptor with the default threshold (1 tick)
// var defaultInterceptor = test.CreateDefaultSlowQueryInterceptor();
//
// // Capture a slow query directly
// var sample = test.CaptureSlowQuery(
//   interceptor,
//   \"SELECT * FROM Orders WHERE Status = @p0\",
//   TimeSpan.FromMilliseconds(850)
// );
//
// if (sample != null)
// {
//   Console.WriteLine($\"Captured: {sample.Duration.TotalMilliseconds}ms\");
//   Console.WriteLine(sample.Sql);
// }
//
// // Get all captured slow queries
// var queries = test.GetSlowQuerySamples(interceptor);
// Console.WriteLine($\"Total slow queries: {queries.Count}\");
//
// // Get the count of slow queries
// int count = test.GetSlowQueryCount(interceptor);
// Console.WriteLine($\"Slow query count: {count}\");
//
// // Clear all recorded slow queries
// foreach (var q in queries)
// {
//   Console.WriteLine($\"{q.Duration.TotalMilliseconds}ms {q.Sql}\");
// }
// test.ClearSlowQueries(interceptor);
// ```
//
// ## IndexSuggestionAnalyzerTestsExtensions
//
// The `IndexSuggestionAnalyzerTestsExtensions` class provides a set of extension methods that invoke the individual test methods of `IndexSuggestionAnalyzerTests`. These helpers make it easy to run a specific test or all tests from regular code without directly referencing the test framework's attributes.
//
// ### Usage Example
//
// ```csharp
// using EfCore.SlowQueryLog.Tests;
//
// var tests = new IndexSuggestionAnalyzerTests();
//
// // Run a single test method
// tests.RunSuggestsIndexForWhereColumn();
//
// // Run all test methods
// tests.RunAllTests();
//
// // Or run a selection of tests individually
// tests.RunSqlWithoutFiltersYieldsNoSuggestions();
// tests.RunSuggestsJoinAndOrderColumns();
// tests.RunParameterMarkersAreNotTreatedAsColumns();
// tests.RunNumericLiteralsAreNotTreatedAsColumns();
// tests.RunOrderByOrdinalIsNotTreatedAsColumn();
// tests.RunToSqlHintBuildsCreateIndexStatement();
// ```
//
// This example demonstrates how to instantiate the test class and invoke its
// validation logic through the provided extension methods.
//
// ## License
//
// MIT
