# EfCore.SlowQueryLog

EF Core `DbCommandInterceptor` (net8.0, NuGet package v0.4.0) that logs, ranks and explains slow queries with generated SQL and regex-based index suggestions.

## Build

```bash
dotnet restore EfCore.SlowQueryLog.sln
dotnet build EfCore.SlowQueryLog.sln -c Release
dotnet pack src/EfCore.SlowQueryLog/EfCore.SlowQueryLog.csproj -c Release
```

SDK: .NET 8 target; builds with any newer SDK (tests use `RollForward=LatestMajor`). Build currently passes with warnings (xUnit1031 and similar); do not add new warnings.

## Test

```bash
dotnet test EfCore.SlowQueryLog.sln
dotnet test tests/EfCore.SlowQueryLog.Tests --filter "FullyQualifiedName~SlowQueryInterceptor"
```

xUnit 2.8, SQLite in-memory (`Microsoft.EntityFrameworkCore.Sqlite`) for end-to-end tests. `SlowQueryInterceptor.Capture(command, duration)` is public so most tests drive the interceptor without a database.

## Lint / format

No `.editorconfig`, analyzers or CI config. `Nullable` and `ImplicitUsings` are enabled; `GenerateDocumentationFile` is on with CS1591 suppressed. Use `dotnet format EfCore.SlowQueryLog.sln` for whitespace/style if needed.

## Layout

- `src/EfCore.SlowQueryLog/` - the library (only project in the package)
  - `Interception/SlowQueryInterceptor.cs` - core: overrides all six `*Executed[Async]` hooks, funnels into `Capture()` (threshold check, sample build, ranking insert, log, `OnSlowQuery` callback)
  - `Options/SlowQueryLogOptions.cs` - `Threshold` (500 ms), `LogLevel`, `IncludeParameterValues`, `SuggestIndexes`, `RankingCapacity` (25), `OnSlowQuery`; `Validate()` plus `*Validator`/`*Validation` helpers
  - `Reporting/` - `SlowQueryRanking` (bounded, lock-based, by duration), `SlowQueryFingerprintRanking` (grouped by SQL, avg/total/p95/max metric), `SlowQueryReportWriter`, `SlowQueryMarkdownReportGenerator`, `SqlSanitizer`, `ISlowQueryRanking`, `SlowQueryRankingFactory`
  - `Analysis/` - `IndexSuggestionAnalyzer` (regex, not a SQL parser), `IndexSuggestionAggregator`, `IndexSuggestionBackgroundAnalyzer`, `RegexParsingException`
  - `SlowQuerySample.cs` - immutable `SlowQuerySample` / `IndexSuggestion` records
  - `SlowQueryLogExtensions.cs` - `UseSlowQueryLog(...)` on `DbContextOptionsBuilder` (options delegate overload, and pre-built interceptor overload so callers keep the `Ranking` handle)
  - `SlowQueryLogServiceCollectionExtensions.cs` - DI registration
- `tests/EfCore.SlowQueryLog.Tests/` - the only test project referenced by the solution
- `docs/ARCHITECTURE.md` - data flow and design decisions; `docs/*.md` - per-class notes
- `README.md` - public usage docs and examples

Stray files not in the solution (leftovers from automated edits, safe to ignore, do not extend): `src/EfCore.SlowQueryLog.Tests/`, `FeatureVerification/`, `verify_features.csx`, `path/to/filename.cs`, root `SlowQueryLogOptions.cs`, `*.backup`, `Program.cs.bak`, and a file literally named `Assert.Equal("SELECT 2", samples[0].Sql);`.

## Conventions

- Namespaces follow folders: `EfCore.SlowQueryLog`, `.Interception`, `.Options`, `.Reporting`, `.Analysis`.
- Extension helpers live in `<Type>Extensions.cs`; JSON helpers in `<Type>JsonExtensions.cs` (System.Text.Json, no extra packages); validation in `<Type>Validation.cs` / `<Type>Validator.cs`.
- Test files mirror source names: `<Type>Tests.cs`, plus `<Type>TestsExtensions.cs` / `<Type>TestsJsonExtensions.cs` / `<Type>TestsValidation.cs` splits. Test method names use underscores: `GetFingerprints_groups_by_sql_and_computes_statistics`.
- Public types get XML doc comments. Samples and suggestions are immutable records; rankings are thread-safe via `lock` and expose `Snapshot()` copies.
- Interceptor uses post-execution hooks only and EF's `eventData.Duration`; never add a stopwatch to the pre-execution path. Below-threshold commands must stay allocation-free.
- Library dependencies are limited to `Microsoft.EntityFrameworkCore.Relational` and `Microsoft.Extensions.Logging.Abstractions`; do not add others without reason.
- Commit messages: conventional prefixes (`docs:`, `chore:`, `feat:`, `fix:`).
