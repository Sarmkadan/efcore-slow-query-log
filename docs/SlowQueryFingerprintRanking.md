# SlowQueryFingerprintRanking

Represents an aggregated ranking entry for a unique query fingerprint, tracking execution statistics, index suggestions, and sample details for slow-query analysis in Entity Framework Core.

## API

### Constructors

#### `public SlowQueryFingerprintRanking(string sql, string? parameters = null)`
Initializes a new ranking entry for the given query fingerprint.

**Parameters**
- `sql` — The normalized SQL text of the fingerprint.
- `parameters` — Optional parameter placeholder string associated with the fingerprint.

**Throws**
- `ArgumentNullException` — If `sql` is null or empty.

#### `public SlowQueryFingerprintRanking(SlowQueryFingerprint fingerprint)`
Initializes a new ranking entry from an existing fingerprint instance.

**Parameters**
- `fingerprint` — The fingerprint to seed the ranking with.

**Throws**
- `ArgumentNullException` — If `fingerprint` is null.

### Properties

#### `public string Sql { get; }`
Gets the normalized SQL text that defines this fingerprint.

#### `public string? Parameters { get; }`
Gets the parameter placeholder string for the fingerprint, or null if not captured.

#### `public IReadOnlyList<IndexSuggestion> Suggestions { get; }`
Gets the read-only collection of index suggestions generated for this fingerprint.

#### `public int SampleCount { get; }`
Gets the total number of execution samples accumulated for this fingerprint.

#### `public TimeSpan AverageDuration { get; }`
Gets the mean execution duration across all samples.

#### `public TimeSpan MaxDuration { get; }`
Gets the longest single execution duration observed.

#### `public TimeSpan MinDuration { get; }`
Gets the shortest single execution duration observed.

#### `public TimeSpan TotalDuration { get; }`
Gets the sum of all execution durations for this fingerprint.

#### `public TimeSpan Percentile95 { get; }`
Gets the 95th-percentile execution duration. Returns `TimeSpan.Zero` until `ComputePercentile95` is called.

#### `public IReadOnlyList<SlowQueryFingerprint> Snapshot { get; }`
Gets a read-only snapshot of all individual samples recorded for this fingerprint.

### Methods

#### `public void Add(SlowQueryFingerprint sample)`
Adds a single execution sample to the ranking, updating aggregate statistics.

**Parameters**
- `sample` — The fingerprint sample to incorporate.

**Throws**
- `ArgumentNullException` — If `sample` is null.
- `InvalidOperationException` — If `sample.Sql` does not match this ranking's `Sql`.

#### `public void AddRange(IEnumerable<SlowQueryFingerprint> samples)`
Adds multiple execution samples in a single operation.

**Parameters**
- `samples` — The collection of samples to incorporate.

**Throws**
- `ArgumentNullException` — If `samples` is null.
- `InvalidOperationException` — If any sample's `Sql` does not match this ranking's `Sql`.

#### `public void AddSample(TimeSpan duration, DateTimeOffset timestamp, string? connectionId = null)`
Records a raw execution sample without requiring a full `SlowQueryFingerprint` instance.

**Parameters**
- `duration` — The execution duration.
- `timestamp` — When the execution occurred.
- `connectionId` — Optional identifier of the database connection.

**Throws**
- `ArgumentOutOfRangeException` — If `duration` is negative.

#### `public void ComputePercentile95()`
Calculates the 95th-percentile duration from the current sample set and updates `Percentile95`.

**Remarks**
Must be called after all samples are added; the value is not updated automatically.

#### `public void Clear()`
Removes all recorded samples and resets aggregate statistics to their initial state. `Sql`, `Parameters`, and `Suggestions` are preserved.

## Usage

### Aggregating samples from an interceptor

```csharp
var ranking = new SlowQueryFingerprintRanking(
    "SELECT * FROM Orders WHERE CustomerId = @p0",
    "@p0 = 'ALFKI'");

foreach (var execution in slowQueryLogService.GetExecutions())
{
    if (execution.CommandText == ranking.Sql)
    {
        ranking.AddSample(execution.Duration, execution.Timestamp, execution.ConnectionId);
    }
}

ranking.ComputePercentile95();

Console.WriteLine($"Fingerprint: {ranking.Sql}");
Console.WriteLine($"Samples: {ranking.SampleCount}");
Console.WriteLine($"Avg: {ranking.AverageDuration.TotalMilliseconds:F1} ms");
Console.WriteLine($"P95: {ranking.Percentile95.TotalMilliseconds:F1} ms");
```

### Building a report with index suggestions

```csharp
var analyzer = new IndexSuggestionAnalyzer(dbContext);
var rankings = new List<SlowQueryFingerprintRanking>();

foreach (var group in slowQueries.GroupBy(q => q.NormalizedSql))
{
    var ranking = new SlowQueryFingerprintRanking(group.Key);
    ranking.AddRange(group);
    ranking.ComputePercentile95();

    var suggestions = analyzer.Analyze(ranking.Sql, ranking.Parameters);
    foreach (var s in suggestions)
    {
        ranking.Suggestions.Add(s);
    }

    rankings.Add(ranking);
}

var report = rankings
    .OrderByDescending(r => r.Percentile95)
    .Take(10)
    .Select(r => new
    {
        r.Sql,
        r.SampleCount,
        P95Ms = r.Percentile95.TotalMilliseconds,
        Suggestions = r.Suggestions.Select(s => s.CreateStatement).ToList()
    });

File.WriteAllText("slow-query-report.json", JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
```

## Notes

- **Thread safety**: This type is not thread-safe. Concurrent calls to `Add`, `AddRange`, `AddSample`, or `Clear` from multiple threads will corrupt internal state. Synchronize externally or use a dedicated instance per thread.
- **Percentile calculation**: `Percentile95` remains `TimeSpan.Zero` until `ComputePercentile95` is explicitly invoked. Call it after all samples for a reporting window have been added.
- **Sample immutability**: `Snapshot` returns a read-only view of the internal sample list. Modifying the returned list is not possible; however, the contained `SlowQueryFingerprint` instances are mutable reference types.
- **Sql matching**: `Add` and `AddRange` validate that incoming samples share the same `Sql` value. This prevents accidental mixing of different fingerprints but requires callers to ensure normalization is consistent.
- **Clearing samples**: `Clear` resets `SampleCount`, all duration aggregates, `Percentile95`, and the internal sample list. It does not affect `Sql`, `Parameters`, or `Suggestions`, allowing the ranking object to be reused for a new time window.
- **Empty state**: A newly constructed instance has `SampleCount = 0`, all duration properties set to `TimeSpan.Zero`, and an empty `Snapshot`. Calling `ComputePercentile95` in this state leaves `Percentile95` at `TimeSpan.Zero`.
