using System;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Provides extension methods for <see cref="IndexSuggestionAnalyzerTests"/>.
/// </summary>
public static class IndexSuggestionAnalyzerTestsExtensions
{
    /// <summary>
    /// Invokes the <see cref="IndexSuggestionAnalyzerTests.Suggests_index_for_where_column"/> test method.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
    public static void RunSuggestsIndexForWhereColumn(this IndexSuggestionAnalyzerTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        tests.Suggests_index_for_where_column();
    }

    /// <summary>
    /// Invokes the <see cref="IndexSuggestionAnalyzerTests.Sql_without_filters_yields_no_suggestions"/> test method.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
    public static void RunSqlWithoutFiltersYieldsNoSuggestions(this IndexSuggestionAnalyzerTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        tests.Sql_without_filters_yields_no_suggestions();
    }
}
