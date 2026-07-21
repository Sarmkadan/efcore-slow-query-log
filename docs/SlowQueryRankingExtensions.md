# SlowQueryRankingExtensions

`SlowQueryRankingExtensions` provides extension methods for analyzing and ranking slow query logs in Entity Framework Core applications. These methods enable aggregation of query performance metrics, identification of optimization opportunities through index suggestions, and generation of reports in JSON or Markdown formats. The class is designed to work with `SlowQueryLog` instances collected via the `efcore-slow-query-log` infrastructure.

## API

### GetTotalDuration

Calculates the cumulative execution time of all slow queries in the provided log collection.

**Parameters**:
- `logs` (`IEnumerable<SlowQueryLog>`): The source slow query logs.

**Returns**:
- `TimeSpan`: The sum of all query durations.

**Exceptions**:
- `ArgumentNullException`: Thrown when `logs` is null.

---

### GetAverageDuration

Computes the arithmetic mean of query execution times across all slow queries.

**Parameters**:
- `logs` (`IEnumerable<SlowQueryLog>`): The source slow query logs.

**Returns**:
- `double`: The average duration in milliseconds.

**Exceptions**:
- `ArgumentNullException`: Thrown when `logs` is null.
- `InvalidOperationException`: Thrown when `logs` is empty.

---

### GetAllSuggestions

Retrieves index optimization recommendations derived from query patterns in the logs.

**Parameters**:
- `logs` (`IEnumerable<SlowQueryLog>`): The source slow query logs.

**Returns**:
- `IEnumerable<IndexSuggestion>`: A sequence of suggested indexes to improve query performance.

**Exceptions**:
- `ArgumentNullException`: Thrown when `logs` is null.

---

### GetFingerprints

Groups slow queries into unique fingerprints based on their SQL structure and parameters.

**Parameters**:
- `logs` (`IEnumerable<SlowQueryLog>`): The source slow query logs.

**Returns**:
- `IReadOnlyList<SlowQueryFingerprint>`: A list of distinct query fingerprints with aggregated statistics.

**Exceptions**:
- `ArgumentNullException`: Thrown when `logs` is null.

---

### GetFingerprintsByTotalDuration

Orders query fingerprints by descending total execution time.

**Parameters**:
- `logs` (`IEnumerable<SlowQueryLog>`): The source slow query logs.

**Returns**:
- `IReadOnlyList<SlowQueryFingerprint>`: Fingerprints sorted by total duration.

**Exceptions**:
- `ArgumentNullException`: Thrown when `logs` is null.

---

### GetFingerprintsByP95Duration

Orders query fingerprints by descending 95th percentile execution time.

**Parameters**:
- `logs` (`IEnumerable<SlowQueryLog>`): The source slow query logs.

**Returns**:
- `IReadOnlyList<SlowQueryFingerprint>`: Fingerprints sorted by P95 duration.

**Exceptions**:
- `ArgumentNullException`: Thrown when `logs` is null.

---

### GetFingerprintsByMaxDuration

Orders query fingerprints by descending maximum execution time.

**Parameters**:
- `logs` (`IEnumerable<SlowQueryLog>`): The source slow query logs.

**Returns**:
- `IReadOnlyList<SlowQueryFingerprint>`: Fingerprints sorted by peak duration.

**Exceptions**:
- `ArgumentNullException`: Thrown when `logs` is null.

---

### ExportToJson

Serializes slow query logs to a JSON file at the specified path.

**Parameters**:
- `logs` (`IEnumerable<SlowQueryLog>`): The source slow query logs.
- `filePath` (`string`): The output file path.

**Exceptions**:
- `ArgumentNullException`: Thrown when `logs` or `filePath` is null.
- `DirectoryNotFoundException`: Thrown when the directory does not exist.
- `IOException`: Thrown on file access errors.

---

### GenerateMarkdownReport

Creates a Markdown-formatted report summarizing slow query statistics and suggestions.

**Parameters**:
- `logs` (`IEnumerable<SlowQueryLog>`): The source slow query logs.

**Returns**:
- `string`: The Markdown report content.

**Exceptions**:
- `ArgumentNullException`: Thrown when `logs` is null.

---

### WriteMarkdownReport

Writes a Markdown report to the specified file path.

**Parameters**:
- `logs` (`IEnumerable<SlowQueryLog>`): The source slow query logs.
- `filePath` (`string`): The output file path.

**Exceptions**:
- `ArgumentNullException`: Thrown when `logs` or `filePath` is null.
- `DirectoryNotFoundException`: Thrown when the directory does not exist.
- `IOException`: Thrown on file access errors.

## Usage

```csharp
var slowLogs = GetSlowQueryLogs(); // Assume this returns IEnumerable<SlowQueryLog>

// Get top 10 slowest query fingerprints by total duration
var topFingerprints = slowLogs
    .GetFingerprintsByTotalDuration()
    .Take(10);

foreach (var fingerprint in topFingerprints)
{
    Console.WriteLine($"SQL: {fingerprint.SqlSnippet}");
    Console.WriteLine($"Total Duration: {fingerprint.TotalDuration.TotalMilliseconds}ms");
}
```

```csharp
var slowLogs = GetSlowQueryLogs();

// Generate and save a Markdown report
slowLogs.WriteMarkdownReport("slow-query-report.md");

// Or generate report content for further processing
string reportContent = slowLogs.GenerateMarkdownReport();
Console.WriteLine(reportContent);
```

## Notes

- All methods require non-null input collections. Null checks are enforced via `ArgumentNullException`.
- Sorting methods (`GetFingerprintsBy*`) return stable orderings but may have undefined behavior if underlying collections are modified during enumeration.
- `GetAverageDuration` throws if the input collection is empty; callers should validate input or handle the exception.
- File-based operations (`ExportToJson`, `WriteMarkdownReport`) delegate to standard .NET I/O APIs and inherit their thread-safety characteristics. Concurrent writes to the same file path may cause `IOException`.
- Index suggestions are derived from query structure analysis and do not account for database-specific indexing constraints or existing indexes.
