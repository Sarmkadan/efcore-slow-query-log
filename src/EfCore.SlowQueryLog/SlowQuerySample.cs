namespace EfCore.SlowQueryLog;

/// <summary>
/// A single captured slow query, including the generated SQL, timing and any
/// index suggestions the analyzer produced.
/// </summary>
public sealed record SlowQuerySample
{
    public required string Sql { get; init; }

    public required TimeSpan Duration { get; init; }

    public required DateTimeOffset CapturedAt { get; init; }

    /// <summary>Formatted parameter list, or null when parameter capture is disabled.</summary>
    public string? Parameters { get; init; }

    /// <summary>Index suggestions produced from the SQL. Never null; may be empty.</summary>
    public IReadOnlyList<IndexSuggestion> Suggestions { get; init; } = Array.Empty<IndexSuggestion>();
}

/// <summary>
/// A naive index recommendation derived from a filter / join / sort column.
/// </summary>
public sealed record IndexSuggestion(string Table, IReadOnlyList<string> Columns, string Reason, IReadOnlyList<string>? IncludeColumns = null)
{
    public string ToSqlHint()
    {
        var cols = string.Join(", ", Columns);
        var name = $"IX_{Table}_{string.Join("_", Columns)}".Replace(".", "_");
        if (IncludeColumns != null && IncludeColumns.Count > 0)
        {
            var includes = string.Join(", ", IncludeColumns);
            return $"CREATE INDEX {name} ON {Table} ({cols}) INCLUDE ({includes});";
        }
        return $"CREATE INDEX {name} ON {Table} ({cols});";
    }
}
