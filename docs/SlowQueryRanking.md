# SlowQueryRanking

The `SlowQueryRanking` class accumulates slow query samples and provides aggregated metrics, unique query fingerprints, and index suggestions. It is designed to track and analyze slow queries over time, enabling performance diagnostics and optimization recommendations.

## API

### `public SlowQueryRanking()`
### `public SlowQueryRanking(…)`

Initializes a new instance of the `SlowQueryRanking` class. Two constructor overloads are available; refer to the source code for parameter details.

### `public void Add(SlowQuerySample sample)`

Adds a slow query sample to the ranking.

- **Parameters**  
  `sample` – The `SlowQuerySample` to add. Must not be `null`.
- **Returns**  
  `void`
- **Throws**  
  `ArgumentNullException` if `sample` is `null`.

### `public IReadOnlyList<SlowQuerySample> Snapshot`

Gets a read-only snapshot of all samples added so far. The returned list is a copy of the internal collection at the time of the call; subsequent additions are not reflected.

- **Returns**  
  `IReadOnlyList<SlowQuerySample>` – A read-only list of the current samples.

### `public void Clear()`

Removes all samples from the ranking, resetting all aggregated metrics.

- **Returns**  
  `void`

### `public TimeSpan GetTotalDuration()`

Returns the total duration of all samples currently in the ranking.

- **Returns**  
  `TimeSpan` – The sum of the durations of all added samples. Returns `TimeSpan.Zero` if no samples have been added.

### `public double GetAverageDuration()`

Returns the average duration of all samples in the ranking.

- **Returns**  
  `double` – The average duration (in milliseconds). Returns `0.0` if no samples have been added.

### `public IEnumerable<IndexSuggestion> GetAllSuggestions()`

Returns all index suggestions derived from the collected slow query samples.

- **Returns**  
  `IEnumerable<IndexSuggestion>` – An enumeration of index suggestions. Returns an empty sequence if no suggestions are available.

### `public IReadOnlyList<SlowQueryFingerprint> GetFingerprints()`

Returns a list of unique fingerprints for the slow queries in the ranking. Each fingerprint represents a distinct query pattern.

- **Returns**  
  `IReadOnlyList<SlowQueryFingerprint>` – A read-only list of fingerprints. Returns an empty list if no samples have been added.

## Usage

### Example 1: Basic collection and metrics

```csharp
var ranking = new SlowQueryRanking();

ranking.Add(new SlowQuerySample("SELECT * FROM Orders", TimeSpan.FromMilliseconds(500)));
ranking.Add(new SlowQuerySample("SELECT * FROM Customers", TimeSpan.FromMilliseconds(1200)));
ranking.Add(new SlowQuerySample("SELECT * FROM Orders", TimeSpan.FromMilliseconds(800)));

var snapshot = ranking.Snapshot;
Console.WriteLine($"Total samples: {snapshot.Count}");
Console.WriteLine($"Total duration: {ranking.GetTotalDuration()}");
Console.WriteLine($"Average duration: {ranking.GetAverageDuration()} ms");

var fingerprints = ranking.GetFingerprints();
Console.WriteLine($"Unique query patterns: {fingerprints.Count}");

foreach (var suggestion in ranking.GetAllSuggestions())
{
    Console.WriteLine($"Suggested index: {suggestion.IndexName}");
}
```

### Example 2: Clearing and reusing the ranking

```csharp
var ranking = new SlowQueryRanking();

// Simulate collecting samples over a time window
ranking.Add(new SlowQuerySample("SELECT * FROM Products", TimeSpan.FromSeconds(2)));
ranking.Add(new SlowQuerySample("SELECT * FROM Products", TimeSpan.FromSeconds(3)));

Console.WriteLine($"Before clear - total duration: {ranking.GetTotalDuration()}");

ranking.Clear();

Console.WriteLine($"After clear - total duration: {ranking.GetTotalDuration()}"); // 00:00:00
Console.WriteLine($"After clear - average duration: {ranking.GetAverageDuration()}"); // 0
Console.WriteLine($"After clear - fingerprints count: {ranking.GetFingerprints().Count}"); // 0
```

## Notes

- **Empty ranking** – When no samples have been added, `GetTotalDuration()` returns `TimeSpan.Zero`, `GetAverageDuration()` returns `0.0`, `GetFingerprints()` returns an empty list, and `GetAllSuggestions()` returns an empty sequence.
- **Thread safety** – This type is not thread-safe. Concurrent calls to `Add`, `Clear`, or any read methods from multiple threads may result in inconsistent state. External synchronization (e.g., a lock) is required if the instance is shared across threads.
- **Snapshot behavior** – The `Snapshot` property returns a read-only copy of the internal collection at the moment of access. It does not reflect subsequent additions or removals. To obtain an updated view, call `Snapshot` again after modifications.
- **Null arguments** – Passing `null` to `Add` throws `ArgumentNullException`.
