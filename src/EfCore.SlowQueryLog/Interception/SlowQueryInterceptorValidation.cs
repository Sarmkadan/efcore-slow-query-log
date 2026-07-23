using System.Data.Common;
using EfCore.SlowQueryLog.Interception;

/// <summary>
/// Provides validation helpers for <see cref="SlowQueryInterceptor"/> instances.
/// </summary>
/// <remarks>
/// This class is obsolete. Validation is now performed through <see cref="SlowQueryLogOptions.Validate()"/>.
/// The interceptor validates its options in the constructor, ensuring misconfiguration fails at startup.
/// </remarks>
[Obsolete("Validation is now handled through SlowQueryLogOptions.Validate(). This class will be removed in a future version.")]
public static class SlowQueryInterceptorValidation
{
    /// <summary>
    /// Validates the specified <see cref="SlowQueryInterceptor"/> instance.
    /// </summary>
    /// <param name="value">The interceptor to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    [Obsolete("Use SlowQueryLogOptions.Validate() instead.")]
    public static IReadOnlyList<string> Validate(this SlowQueryInterceptor? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // The interceptor validates its options in the constructor, so this is now a no-op
        return Array.Empty<string>();
    }

    /// <summary>
    /// Determines whether the specified <see cref="SlowQueryInterceptor"/> instance is valid.
    /// </summary>
    /// <param name="value">The interceptor to check.</param>
    /// <returns><see langword="true"/> if the interceptor is valid; otherwise, <see langword="false"/>.</returns>
    [Obsolete("Use SlowQueryLogOptions.IsValid() instead.")]
    public static bool IsValid(this SlowQueryInterceptor? value) => value is not null;

    /// <summary>
    /// Ensures that the specified <see cref="SlowQueryInterceptor"/> instance is valid.
    /// </summary>
    /// <param name="value">The interceptor to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    [Obsolete("Validation is now performed during interceptor construction.")]
    public static void EnsureValid(this SlowQueryInterceptor? value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }
}