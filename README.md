## SlowQueryRankingFingerprintTests

The `SlowQueryRankingFingerprintTests` class contains methods for testing slow query ranking fingerprints. It provides methods to get fingerprints groups by SQL and computes statistics, as well as methods to get fingerprints by total duration, P95 duration, and max duration. For example:
```csharp
public void GetFingerprints_groups_by_sql_and_computes_statistics
public void GetFingerprintsByTotalDuration_orders_by_total_duration
public void GetFingerprintsByP95Duration_orders_by_p95_duration
public void GetFingerprintsByMaxDuration_orders_by_max_duration
public void P95_calculation_works_correctly
```

## SlowQueryFingerprintRanking

The `SlowQueryFingerprintRanking` class maintains a thread‑safe, bounded ranking of slow‑query fingerprints, grouping samples by their SQL text and ordering them according to a configurable metric (average, total, P95, or max duration). It can ingest individual `SlowQuerySample` instances or collections, and provides snapshot access to the aggregated fingerprints for reporting or analysis.

```csharp
using System;
using System.Collections.Generic;
using EfCore.SlowQueryLog.Analysis;
using EfCore.SlowQueryLog.Reporting;

class Program
{
    static void Main()
    {
        // Create a ranking that keeps the top 10 fingerprints ordered by average duration
        var ranking = new SlowQueryFingerprintRanking(
            capacity: 10,
            metric: SlowQueryFingerprintRanking.RankingMetric.AverageDuration);

        // Add a single sample
        ranking.Add(new SlowQuerySample
        {
            Sql = "SELECT * FROM Users WHERE Id = @id",
            Parameters = "@id=1",
            Duration = TimeSpan.FromMilliseconds(150),
            Suggestions = Array.Empty<IndexSuggestion>()
        });

        // Add multiple samples at once
        var samples = new List<SlowQuerySample>
        {
            new SlowQuerySample
            {
                Sql = "SELECT * FROM Orders",
                Duration = TimeSpan.FromMilliseconds(300),
                Suggestions = Array.Empty<IndexSuggestion>()
            },
            new SlowQuerySample
            {
                Sql = "SELECT * FROM Users WHERE Id = @id",
                Duration = TimeSpan.FromMilliseconds(200),
                Suggestions = Array.Empty<IndexSuggestion>()
            }
        };
        ranking.AddRange(samples);

        // Get a snapshot of the current fingerprints
        IReadOnlyList<SlowQueryFingerprint> fingerprints = ranking.Snapshot();
        foreach (var fp in fingerprints)
        {
            Console.WriteLine($"SQL: {fp.Sql}");
            Console.WriteLine($"Samples: {fp.SampleCount}");
            Console.WriteLine($"Avg Duration: {fp.AverageDuration}");
            Console.WriteLine($"P95 Duration: {fp.Percentile95}");
        }

        // Clear the ranking when finished
        ranking.Clear();
    }
}
```

## SlowQueryRanking

The `SlowQueryRanking` class maintains a thread‑safe collection of individual slow‑query samples. It allows adding samples, retrieving a snapshot of all recorded samples, clearing the collection, and obtaining aggregated fingerprints grouped by SQL.

```csharp
using System;
using System.Collections.Generic;
using EfCore.SlowQueryLog.Analysis;
using EfCore.SlowQueryLog.Reporting;

class Program
{
    static void Main()
    {
        // Create a ranking that keeps up to 100 samples
        var ranking = new SlowQueryRanking(capacity: 100);

        // Add a single sample
        ranking.Add(new SlowQuerySample
        {
            Sql = "SELECT * FROM Users WHERE Id = @id",
            Parameters = "@id=1",
            Duration = TimeSpan.FromMilliseconds(150),
            Suggestions = Array.Empty<IndexSuggestion>()
        });

        // Add multiple samples
        var samples = new List<SlowQuerySample>
        {
            new SlowQuerySample
            {
                Sql = "SELECT * FROM Orders",
                Duration = TimeSpan.FromMilliseconds(300),
                Suggestions = Array.Empty<IndexSuggestion>()
            },
            new SlowQuerySample
            {
                Sql = "SELECT * FROM Users WHERE Id = @id",
                Duration = TimeSpan.FromMilliseconds(200),
                Suggestions = Array.Empty<IndexSuggestion>()
            }
        };
        foreach (var s in samples)
            ranking.Add(s);

        // Get a snapshot of all samples added so far
        IReadOnlyList<SlowQuerySample> snapshot = ranking.Snapshot();

        // Retrieve aggregated fingerprints (grouped by SQL)
        IReadOnlyList<SlowQueryFingerprint> fingerprints = ranking.GetFingerprints();

        // When done, clear the ranking
        ranking.Clear();
    }
}
```

## SlowQuerySampleJsonTests

`SlowQuerySampleJsonTests` contains a comprehensive suite of unit tests that verify the JSON serialization and deserialization behavior of `SlowQuerySample`. The tests cover formatting options, handling of `null` parameters, empty suggestion collections, special characters, whitespace handling, and error conditions, ensuring round‑trip fidelity and robust error reporting.

```csharp
using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Analysis;
using EfCore.SlowQueryLog.Tests; // Adjust namespace if necessary

class JsonTestDemo
{
    static void Main()
    {
        // Instantiate the test class
        var jsonTests = new SlowQuerySampleJsonTests();

        // Run a few representative test methods manually
        jsonTests.ToJson_SerializesAllFieldsCorrectly();
        jsonTests.ToJson_WithIndentedFormat_ProducesFormattedJson();
        jsonTests.FromJson_RoundtripPreservesAllFields();
        jsonTests.FromJson_WithNullParameters_DeserializesCorrectly();
        jsonTests.TryFromJson_ValidJson_ReturnsTrueAndDeserializes();

        // The above calls exercise the public members of the test class.
        // In a real test run, a test runner (e.g., xUnit, NUnit) would invoke all methods automatically.
    }
}
```

The example demonstrates how the test class can be instantiated and its public test methods invoked directly, which in turn validate the JSON handling logic of `SlowQuerySample`.
```

## SlowQueryInterceptorValidationTests

`SlowQueryInterceptorValidationTests` contains unit tests that verify the behavior of the legacy `SlowQueryInterceptorValidation` helper used to inspect `SlowQueryInterceptor` instances. The tests confirm that validating a properly constructed interceptor produces an empty, non-null, read-only collection of issues, that `IsValid` returns `true` for valid interceptors (including right after construction) and `false` for `null`, and that `EnsureValid` completes without throwing for valid instances while raising an `ArgumentNullException` when passed `null`.

```csharp
using EfCore.SlowQueryLog.Tests; // Adjust namespace if necessary

class ValidationTestDemo
{
    static void Main()
    {
        // Instantiate the test class
        var validationTests = new SlowQueryInterceptorValidationTests();

        // Run a few representative test methods manually
        validationTests.Validate_WithValidInterceptor_ReturnsEmptyList();
        validationTests.Validate_WithNullInterceptor_ThrowsArgumentNullException();
        validationTests.IsValid_WithValidInterceptor_ReturnsTrue();
        validationTests.IsValid_WithNullInterceptor_ReturnsFalse();
        validationTests.EnsureValid_WithValidInterceptor_DoesNotThrow();
        validationTests.EnsureValid_WithNullInterceptor_ThrowsArgumentNullException();

        // The above calls exercise the public members of the test class.
        // In a real test run, a test runner (e.g., xUnit) would invoke all methods automatically.
    }
}
```

## SlowQueryFingerprintRankingTests

`SlowQueryFingerprintRankingTests` contains unit tests that verify the behavior of `SlowQueryFingerprintRanking`, which maintains a thread-safe, bounded ranking of slow-query fingerprints grouped by SQL text. The tests confirm that constructing a ranking with a zero capacity throws an `ArgumentOutOfRangeException`, that samples added individually or via `AddRange` are grouped by SQL with correctly aggregated statistics (while null samples or collections are rejected), that the configured capacity keeps only the top-ranked fingerprints, and that `Clear` removes all fingerprints while the `Metric` property reports the metric the ranking was created with.

```csharp
using EfCore.SlowQueryLog.Tests; // Adjust namespace if necessary

class FingerprintRankingTestDemo
{
    static void Main()
    {
        // Instantiate the test class
        var rankingTests = new SlowQueryFingerprintRankingTests();

        // Run a few representative test methods manually
        rankingTests.Constructor_ZeroCapacity_ThrowsArgumentOutOfRangeException();
        rankingTests.Add_SingleSample_CreatesFingerprintWithCorrectStatistics();
        rankingTests.Add_MultipleSamples_GroupedBySql_AggregatesStatistics();
        rankingTests.Add_DifferentSql_CreatesSeparateFingerprints();
        rankingTests.AddRange_EmptyCollection_NoEffect();
        rankingTests.AddRange_NullCollection_ThrowsArgumentNullException();
        rankingTests.Capacity_IsRespected_OnlyTopRankedKept();
        rankingTests.Clear_RemovesAllFingerprints();
        rankingTests.MetricProperty_ReturnsConfiguredMetric();
        rankingTests.Add_NullSample_ThrowsArgumentNullException();

        // The above calls exercise the public members of the test class.
        // In a real test run, a test runner (e.g., xUnit) would invoke all methods automatically.
    }
}
```

## SlowQueryRankingTests

The `SlowQueryRankingTests` class contains unit tests that verify the behavior of `SlowQueryRanking`, which maintains a thread-safe, bounded collection of slow-query samples ordered by duration. The tests confirm that samples are ordered by duration descending, capacity limits are respected keeping only the slowest samples, percentiles are computed correctly, fingerprints are grouped by SQL with proper statistics, and the class is thread-safe for concurrent access.

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Reporting;

class SlowQueryRankingTestDemo
{
    static void Main()
    {
        // Create a ranking that keeps up to 5 samples
        var ranking = new SlowQueryRanking(capacity: 5);

        // Add samples with different durations
        ranking.Add(new SlowQuerySample
        {
            Sql = "SELECT * FROM Products",
            Duration = TimeSpan.FromMilliseconds(100),
            CapturedAt = DateTimeOffset.UtcNow
        });

        ranking.Add(new SlowQuerySample
        {
            Sql = "SELECT * FROM Orders WHERE CustomerId = @id",
            Duration = TimeSpan.FromMilliseconds(500),
            CapturedAt = DateTimeOffset.UtcNow
        });

        ranking.Add(new SlowQuerySample
        {
            Sql = "SELECT * FROM Users",
            Duration = TimeSpan.FromMilliseconds(300),
            CapturedAt = DateTimeOffset.UtcNow
        });

        // Get a snapshot of all samples (ordered by duration descending)
        IReadOnlyList<SlowQuerySample> snapshot = ranking.Snapshot();
        
        // Should be ordered: 500ms, 300ms, 100ms
        Console.WriteLine($"Top query: {snapshot[0].Sql} ({snapshot[0].Duration.TotalMilliseconds}ms)");
        Console.WriteLine($"Second query: {snapshot[1].Sql} ({snapshot[1].Duration.TotalMilliseconds}ms)");
        Console.WriteLine($"Third query: {snapshot[2].Sql} ({snapshot[2].Duration.TotalMilliseconds}ms)");

        // Get fingerprints grouped by SQL
        IReadOnlyList<SlowQueryFingerprint> fingerprints = ranking.GetFingerprints();
        foreach (var fp in fingerprints)
        {
            Console.WriteLine($"SQL: {fp.Sql}");
            Console.WriteLine($"Sample count: {fp.SampleCount}");
            Console.WriteLine($"Average duration: {fp.AverageDuration.TotalMilliseconds}ms");
        }

        // When done, clear the ranking
        ranking.Clear();
    }
}
```

## SlowQueryInterceptorExtensionsTests

`SlowQueryInterceptorExtensionsTests` contains a comprehensive suite of unit tests that verify the behavior of the `SlowQueryInterceptor` extension methods. The tests confirm that capturing queries correctly updates the ranking, that querying captured data returns accurate counts and ordered results, and that edge cases like empty states or null interceptors are handled appropriately.

```csharp
using EfCore.SlowQueryLog.Tests; // Adjust namespace if necessary

class InterceptorExtensionsTestDemo
{
    static void Main()
    {
        // Instantiate the test class
        var extensionsTests = new SlowQueryInterceptorExtensionsTests();

        // Run a few representative test methods manually
        extensionsTests.Capture_adds_query_to_ranking();
        extensionsTests.Capture_returns_null_when_below_threshold();
        extensionsTests.GetCapturedQueries_returns_queries_ordered_by_duration();
        extensionsTests.GetSlowestQuery_returns_slowest_query();
        extensionsTests.GetFastestQuery_returns_fastest_query();
        extensionsTests.GetQueryCount_returns_correct_count();
        extensionsTests.HasCapturedQueries_returns_true_when_has_queries();
        extensionsTests.Clear_removes_all_captured_queries();

        // The above calls exercise the public members of the test class.
        // In a real test run, a test runner (e.g., xUnit) would invoke all methods automatically.
    }
}
```


## SlowQueryLogServiceCollectionExtensionsTests

`SlowQueryLogServiceCollectionExtensionsTests` contains unit tests that verify the behavior of the `AddSlowQueryLog` extension methods for `IServiceCollection`. The tests cover registering the interceptor with no arguments, with a configuration action, with a provided interceptor instance, and proper argument null checking.

```csharp
using EfCore.SlowQueryLog.Tests; // Adjust namespace if necessary

class ServiceCollectionExtensionsTestDemo
{
    static void Main()
    {
        // Instantiate the test class
        var tests = new SlowQueryLogServiceCollectionExtensionsTests();

        // Run a few representative test methods manually
        tests.AddSlowQueryLog_NoArgs_RegistersInterceptor();
        tests.AddSlowQueryLog_WithConfigure_RegistersInterceptorAndInvokesConfigure();
        tests.AddSlowQueryLog_WithNullServices_ThrowsArgumentNullException();
        tests.AddSlowQueryLog_WithInterceptor_RegistersProvidedInstance();
        tests.AddSlowQueryLog_WithNullInterceptor_ThrowsArgumentNullException();

        // The above calls exercise the public members of the test class.
        // In a real test run, a test runner (e.g., xUnit) would invoke all methods automatically.
    }
}
```