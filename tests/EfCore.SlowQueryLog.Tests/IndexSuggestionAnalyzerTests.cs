using EfCore.SlowQueryLog.Analysis;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

public class IndexSuggestionAnalyzerTests
{
    private readonly IndexSuggestionAnalyzer _analyzer = new();

    [Fact]
    public void Suggests_index_for_where_column()
    {
        var sql = "SELECT [c].[Id], [c].[Email] FROM [Customers] AS [c] WHERE [c].[Email] = @p0";

        var suggestions = _analyzer.Analyze(sql);

        var s = Assert.Single(suggestions);
        Assert.Equal("Customers", s.Table);
        Assert.Contains("Email", s.Columns);
    }

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

    [Fact]
    public void Empty_sql_yields_no_suggestions()
    {
        Assert.Empty(_analyzer.Analyze(""));
        Assert.Empty(_analyzer.Analyze("   "));
    }

    [Fact]
    public void Sql_without_filters_yields_no_suggestions()
    {
        var sql = "SELECT [c].[Id] FROM [Customers] AS [c]";
        Assert.Empty(_analyzer.Analyze(sql));
    }

    [Fact]
    public void ToSqlHint_builds_create_index_statement()
    {
        var suggestion = new IndexSuggestion("Orders", new[] { "CustomerId", "Status" }, "test");
        Assert.Equal("CREATE INDEX IX_Orders_CustomerId_Status ON Orders (CustomerId, Status);", suggestion.ToSqlHint());
    }
}
