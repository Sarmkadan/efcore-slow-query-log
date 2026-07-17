# SlowQuerySample

The `SlowQuerySample` type encapsulates data related to a database query execution that has been identified as exceeding a defined latency threshold within an Entity Framework Core application. It provides access to the original SQL command, its execution duration, the capture timestamp, associated query parameters, and actionable index optimization recommendations derived from the query's structure.

## API

### Sql
`public required string Sql`
The complete SQL command text that was executed against the database.

### Duration
`public required TimeSpan Duration`
The total time elapsed during the execution of the query.

### CapturedAt
`public required DateTimeOffset CapturedAt`
The point in time at which the slow query was observed and logged.

### Parameters
`public string? Parameters`
An optional string representation of the query parameters associated with the SQL command, if captured. This value may be `null` if no parameters were used or if parameter logging is disabled.

### Suggestions
`public IReadOnlyList<IndexSuggestion> Suggestions`
A read-only collection of `IndexSuggestion` objects, each representing a potential database index that could optimize the performance of the captured query. If no suggestions are generated, this list will be empty.

### IndexSuggestion
`public sealed record IndexSuggestion`
A data structure representing a recommended index optimization. It contains the metadata necessary to construct or identify a potentially missing database index.

### ToSqlHint
`public string ToSqlHint`
A string containing a SQL hint or comment derived from the available `Suggestions`. This string can be used to document or guide database query planners regarding the recommended optimizations.

## Usage

### Logging Slow Query Metrics
```csharp
public void LogSlowQuery(SlowQuerySample sample)
{
    Console.WriteLine($"Query took {sample.Duration.TotalMilliseconds}ms.");
    Console.WriteLine($"Captured at: {sample.CapturedAt}");
    Console.WriteLine($"SQL: {sample.Sql}");
    
    if (!string.IsNullOrEmpty(sample.Parameters))
    {
        Console.WriteLine($"Parameters: {sample.Parameters}");
    }
}
```

### Applying Optimization Hints
```csharp
public void ProcessSuggestions(SlowQuerySample sample)
{
    if (sample.Suggestions.Count > 0)
    {
        Console.WriteLine("Optimization suggestions found:");
        foreach (var suggestion in sample.Suggestions)
        {
            Console.WriteLine($"- {suggestion}");
        }
        
        Console.WriteLine($"Suggested SQL Hint: {sample.ToSqlHint}");
    }
}
```

## Notes

- **Thread Safety**: `SlowQuerySample` is designed as a data transfer object (DTO). Once initialized with its `required` members, instances are intended to be immutable. As such, they are inherently thread-safe for concurrent read access.
- **Data Completeness**: While `Sql`, `Duration`, and `CapturedAt` are required and guaranteed to be present, consumers must defensively handle the possibility of `Parameters` being `null` and `Suggestions` being an empty collection.
- **Contextual Validity**: The `Suggestions` and the resulting `ToSqlHint` are derived based on static analysis of the query structure. They should be treated as recommendations and validated against the actual database schema and performance characteristics before implementation.
