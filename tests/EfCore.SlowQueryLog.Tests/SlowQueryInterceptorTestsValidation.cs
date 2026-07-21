using System;
using System.Collections.Generic;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Provides validation helpers for <see cref="SlowQueryInterceptorTests"/> instances.
/// </summary>
public static class SlowQueryInterceptorTestsValidation
{
    /// <summary>
    /// Validates the specified <see cref="SlowQueryInterceptorTests"/> instance.
    /// </summary>
    /// <param name="value">The test fixture to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this SlowQueryInterceptorTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate that all test methods can execute without throwing
        ValidateTestMethod(value.Query_below_threshold_not_recorded, nameof(value.Query_below_threshold_not_recorded), problems);
        ValidateTestMethod(value.Query_at_threshold_is_recorded, nameof(value.Query_at_threshold_is_recorded), problems);
        ValidateTestMethod(value.Query_above_threshold_is_recorded, nameof(value.Query_above_threshold_is_recorded), problems);
        ValidateTestMethod(value.Sample_sql_field_is_populated, nameof(value.Sample_sql_field_is_populated), problems);
        ValidateTestMethod(value.Sample_duration_field_is_populated, nameof(value.Sample_duration_field_is_populated), problems);
        ValidateTestMethod(value.Sample_captured_at_field_is_populated, nameof(value.Sample_captured_at_field_is_populated), problems);
        ValidateTestMethod(value.Parameters_captured_only_when_enabled, nameof(value.Parameters_captured_only_when_enabled), problems);
        ValidateTestMethod(value.RedactParameters_works, nameof(value.RedactParameters_works), problems);
        ValidateTestMethod(value.OnSlowQuery_callback_is_invoked, nameof(value.OnSlowQuery_callback_is_invoked), problems);
        ValidateTestMethod(value.Suggestions_disabled_produces_none, nameof(value.Suggestions_disabled_produces_none), problems);
        ValidateTestMethod(value.Invalid_threshold_throws, nameof(value.Invalid_threshold_throws), problems);
        ValidateTestMethod(value.OnSlowQuery_callback_exception_does_not_break_execution, nameof(value.OnSlowQuery_callback_exception_does_not_break_execution), problems);
        ValidateTestMethod(value.Provider_specific_threshold_overrides_default, nameof(value.Provider_specific_threshold_overrides_default), problems);
        ValidateTestMethod(value.Multiple_queries_ranked_by_duration, nameof(value.Multiple_queries_ranked_by_duration), problems);
        ValidateTestMethod(value.Ranking_capacity_limits_samples, nameof(value.Ranking_capacity_limits_samples), problems);

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Validates a single test method execution.
    /// </summary>
    /// <param name="testMethod">The test method to execute.</param>
    /// <param name="methodName">The name of the method for error reporting.</param>
    /// <param name="problems">The list to add validation problems to.</param>
    private static void ValidateTestMethod(Action testMethod, string methodName, List<string> problems)
    {
        try
        {
            testMethod();
        }
        catch (Exception ex)
        {
            problems.Add($"Test method {methodName} failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Determines whether the specified <see cref="SlowQueryInterceptorTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The test fixture to check.</param>
    /// <returns><see langword="true"/> if the test fixture is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this SlowQueryInterceptorTests? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="SlowQueryInterceptorTests"/> instance is valid.
    /// </summary>
    /// <param name="value">The test fixture to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the test fixture has validation problems.</exception>
    public static void EnsureValid(this SlowQueryInterceptorTests? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"SlowQueryInterceptorTests is not valid. Problems: {string.Join(" ", problems)}");
    }
}