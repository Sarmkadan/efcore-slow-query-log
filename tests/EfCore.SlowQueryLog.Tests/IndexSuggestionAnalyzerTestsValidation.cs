using System;
using System.Collections.Generic;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Provides validation methods for <see cref="IndexSuggestionAnalyzerTests"/> instances.
/// </summary>
public static class IndexSuggestionAnalyzerTestsValidation
{
    /// <summary>
    /// Validates the <see cref="IndexSuggestionAnalyzerTests"/> instance by invoking all test methods.
    /// </summary>
    /// <param name="value">The test instance to validate.</param>
    /// <returns>A list of human-readable problem descriptions. Empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this IndexSuggestionAnalyzerTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Invoke each test method to ensure it executes without throwing
        // These methods test the IndexSuggestionAnalyzer functionality
        try
        {
            value.Suggests_index_for_where_column();
        }
        catch (Exception ex)
        {
            problems.Add($"Method Suggests_index_for_where_column failed: {ex.Message}");
        }

        try
        {
            value.Suggests_join_and_order_columns();
        }
        catch (Exception ex)
        {
            problems.Add($"Method Suggests_join_and_order_columns failed: {ex.Message}");
        }

        try
        {
            value.Empty_sql_yields_no_suggestions();
        }
        catch (Exception ex)
        {
            problems.Add($"Method Empty_sql_yields_no_suggestions failed: {ex.Message}");
        }

        try
        {
            value.Sql_without_filters_yields_no_suggestions();
        }
        catch (Exception ex)
        {
            problems.Add($"Method Sql_without_filters_yields_no_suggestions failed: {ex.Message}");
        }

        try
        {
            value.ToSqlHint_builds_create_index_statement();
        }
        catch (Exception ex)
        {
            problems.Add($"Method ToSqlHint_builds_create_index_statement failed: {ex.Message}");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the <see cref="IndexSuggestionAnalyzerTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The test instance to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this IndexSuggestionAnalyzerTests value)
        => value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the <see cref="IndexSuggestionAnalyzerTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The test instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this IndexSuggestionAnalyzerTests value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"IndexSuggestionAnalyzerTests instance is not valid. Problems: {string.Join(", ", problems)}");
        }
    }
}