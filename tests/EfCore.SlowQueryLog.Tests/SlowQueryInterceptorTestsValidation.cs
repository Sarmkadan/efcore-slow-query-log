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

        return problems.AsReadOnly();
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