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
