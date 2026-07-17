using EfCore.SlowQueryLog.Analysis;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Unit tests for <see cref="IndexSuggestionAnalyzer"/> that verify index suggestion generation from SQL queries.
/// </summary>
public class IndexSuggestionAnalyzerTests
{
    private readonly IndexSuggestionAnalyzer _analyzer = new();

    /// <summary>
    /// Tests that the analyzer suggests an index for a simple WHERE clause filtering on a single column.
    /// </summary>
    [Fact]
    public void Suggests_index_for_where_column()
    {
        var sql = "SELECT [c].[Id], [c].[Email] FROM [Customers] AS [c] WHERE [c].[Email] = @p0";

        var suggestions = _analyzer.Analyze(sql);

        var s = Assert.Single(suggestions);
        Assert.Equal("Customers", s.Table);
        Assert.Contains("Email", s.Columns);
    }

    /// <summary>
    /// Tests that the analyzer suggests an index covering columns used in WHERE clause, JOIN condition, and ORDER BY clause.
    /// </summary>
    [Fact]
    public void Suggests_join_and_order_columns()
    {
        var sql = @"SELECT [o].[Id] FROM [Orders] AS [o]
                    INNER JOIN [Customers] AS [c] ON [o].[CustomerId] = [c].[Id]
                    WHERE [o].[Status] = @p0
                    ORDER BY [o].[CreatedAt]";

        var suggestions = _analyzer.Analyze(sql);

        var orders = Assert.Single(suggestions, x => x.Table == "Orders");
        Assert.Contains("CustomerId", orders.Columns);
        Assert.Contains("Status", orders.Columns);
        Assert.Contains("CreatedAt", orders.Columns);
    }

    /// <summary>
    /// Tests that empty SQL strings and whitespace-only strings return no index suggestions.
    /// </summary>
    [Fact]
    public void Empty_sql_yields_no_suggestions()
    {
        Assert.Empty(_analyzer.Analyze(""));
        Assert.Empty(_analyzer.Analyze("   "));
    }

    /// <summary>
    /// Tests that SQL queries without WHERE clause filters return no index suggestions.
    /// </summary>
    [Fact]
    public void Sql_without_filters_yields_no_suggestions()
    {
        var sql = "SELECT [c].[Id] FROM [Customers] AS [c]";
        Assert.Empty(_analyzer.Analyze(sql));
    }

    /// <summary>
    /// Tests that parameter markers like @p0 are not incorrectly identified as column names.
    /// </summary>
    [Fact]
    public void Parameter_markers_are_not_treated_as_columns()
    {
        var sql = "SELECT * FROM [Customers] WHERE @p0 = [Email]";

        var suggestions = _analyzer.Analyze(sql);

        Assert.DoesNotContain(suggestions, s => s.Columns.Contains("p0"));
    }

    /// <summary>
    /// Tests that numeric literals in SQL queries are not incorrectly treated as column names.
    /// </summary>
    [Fact]
    public void Numeric_literals_are_not_treated_as_columns()
    {
        var sql = "SELECT * FROM [Customers] WHERE 1 = 1 AND [Email] = @p0";

        var s = Assert.Single(_analyzer.Analyze(sql));
        Assert.Equal(new[] { "Email" }, s.Columns);
    }

    /// <summary>
    /// Tests that ORDER BY ordinal values (e.g., ORDER BY 1) are not incorrectly treated as column names.
    /// </summary>
    [Fact]
    public void OrderBy_ordinal_is_not_treated_as_column()
    {
        var sql = "SELECT * FROM [Customers] WHERE [Email] = @p0 ORDER BY 1";

        var s = Assert.Single(_analyzer.Analyze(sql));
        Assert.Equal(new[] { "Email" }, s.Columns);
    }

    /// <summary>
    /// Tests that <see cref="IndexSuggestion.ToSqlHint()"/> correctly generates a CREATE INDEX SQL statement from an index suggestion.
    /// </summary>
    [Fact]
    public void ToSqlHint_builds_create_index_statement()
    {
        var suggestion = new IndexSuggestion("Orders", new[] { "CustomerId", "Status" }, "test");
        Assert.Equal("CREATE INDEX IX_Orders_CustomerId_Status ON Orders (CustomerId, Status);", suggestion.ToSqlHint());
    }
}
