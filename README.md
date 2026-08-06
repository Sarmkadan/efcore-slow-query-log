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