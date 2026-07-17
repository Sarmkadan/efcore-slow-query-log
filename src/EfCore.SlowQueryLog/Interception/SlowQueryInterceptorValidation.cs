using System.Data.Common;

namespace EfCore.SlowQueryLog.Interception;

/// <summary>
/// Provides validation helpers for <see cref="SlowQueryInterceptor"/> instances.
/// </summary>
public static class SlowQueryInterceptorValidation
{
    /// <summary>
    /// Validates the specified <see cref="SlowQueryInterceptor"/> instance.
    /// </summary>
    /// <param name="value">The interceptor to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this SlowQueryInterceptor? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Ranking validation
        if (value.Ranking is null)
        {
            problems.Add("Ranking property cannot be null.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="SlowQueryInterceptor"/> instance is valid.
    /// </summary>
    /// <param name="value">The interceptor to check.</param>
    /// <returns><see langword="true"/> if the interceptor is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this SlowQueryInterceptor? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="SlowQueryInterceptor"/> instance is valid.
    /// </summary>
    /// <param name="value">The interceptor to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the interceptor has validation problems.</exception>
    public static void EnsureValid(this SlowQueryInterceptor? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"SlowQueryInterceptor is not valid. Problems: {string.Join(" ", problems)}");
    }
}