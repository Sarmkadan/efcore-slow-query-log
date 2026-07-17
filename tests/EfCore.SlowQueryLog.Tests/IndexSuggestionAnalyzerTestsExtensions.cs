using System;
using Xunit;

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

    /// <summary>
    /// Invokes the <see cref="IndexSuggestionAnalyzerTests.Suggests_join_and_order_columns"/> test method.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
    public static void RunSuggestsJoinAndOrderColumns(this IndexSuggestionAnalyzerTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        tests.Suggests_join_and_order_columns();
    }

    /// <summary>
    /// Invokes the <see cref="IndexSuggestionAnalyzerTests.Empty_sql_yields_no_suggestions"/> test method.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
    public static void RunEmptySqlYieldsNoSuggestions(this IndexSuggestionAnalyzerTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        tests.Empty_sql_yields_no_suggestions();
    }

    /// <summary>
    /// Invokes the <see cref="IndexSuggestionAnalyzerTests.Parameter_markers_are_not_treated_as_columns"/> test method.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
    public static void RunParameterMarkersAreNotTreatedAsColumns(this IndexSuggestionAnalyzerTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        tests.Parameter_markers_are_not_treated_as_columns();
    }

    /// <summary>
    /// Invokes the <see cref="IndexSuggestionAnalyzerTests.Numeric_literals_are_not_treated_as_columns"/> test method.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
    public static void RunNumericLiteralsAreNotTreatedAsColumns(this IndexSuggestionAnalyzerTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        tests.Numeric_literals_are_not_treated_as_columns();
    }

    /// <summary>
    /// Invokes the <see cref="IndexSuggestionAnalyzerTests.OrderBy_ordinal_is_not_treated_as_column"/> test method.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
    public static void RunOrderByOrdinalIsNotTreatedAsColumn(this IndexSuggestionAnalyzerTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        tests.OrderBy_ordinal_is_not_treated_as_column();
    }

    /// <summary>
    /// Invokes the <see cref="IndexSuggestionAnalyzerTests.ToSqlHint_builds_create_index_statement"/> test method.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
    public static void RunToSqlHintBuildsCreateIndexStatement(this IndexSuggestionAnalyzerTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);
        tests.ToSqlHint_builds_create_index_statement();
    }

    /// <summary>
    /// Runs all test methods on the <see cref="IndexSuggestionAnalyzerTests"/> instance.
    /// </summary>
    /// <param name="tests">The test instance.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="tests"/> is null.</exception>
    public static void RunAllTests(this IndexSuggestionAnalyzerTests tests)
    {
        ArgumentNullException.ThrowIfNull(tests);

        tests.RunSuggestsIndexForWhereColumn();
        tests.RunSqlWithoutFiltersYieldsNoSuggestions();
        tests.RunSuggestsJoinAndOrderColumns();
        tests.RunEmptySqlYieldsNoSuggestions();
        tests.RunParameterMarkersAreNotTreatedAsColumns();
        tests.RunNumericLiteralsAreNotTreatedAsColumns();
        tests.RunOrderByOrdinalIsNotTreatedAsColumn();
        tests.RunToSqlHintBuildsCreateIndexStatement();
    }
}
