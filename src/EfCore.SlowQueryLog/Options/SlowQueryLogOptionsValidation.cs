using System;
using System.Collections.Generic;

namespace EfCore.SlowQueryLog.Options;

/// <summary>
/// Provides validation methods for <see cref="SlowQueryLogOptions"/> instances.
/// </summary>
/// <remarks>
/// This class is obsolete. All validation logic has been consolidated into <see cref="SlowQueryLogOptions"/>.
/// Use the instance methods <see cref="SlowQueryLogOptions.Validate()"/>, <see cref="SlowQueryLogOptions.IsValid()"/>
/// and <see cref="SlowQueryLogOptions.EnsureValid()"/> instead.
/// </remarks>
[Obsolete("All validation logic has been consolidated into SlowQueryLogOptions. Use instance methods instead. This class will be removed in a future version.")]
public static class SlowQueryLogOptionsValidation
{
    /// <summary>
    /// Validates the specified options instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <returns>An empty list if the options are valid; otherwise, a list of error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when called on an obsolete class.</exception>
    [Obsolete("Use options.Validate() instead.")]
    public static IReadOnlyList<string> Validate(this SlowQueryLogOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // This obsolete class is kept for backwards compatibility only.
        // In a real application, migrate to using the instance methods directly.
        try
        {
            value.Validate();
            return Array.Empty<string>();
        }
        catch (ArgumentOutOfRangeException ex) when (ex.ParamName != null)
        {
            return new[] { ex.Message };
        }
        catch (ArgumentException ex)
        {
            return new[] { ex.Message };
        }
    }

    /// <summary>
    /// Determines whether the specified options instance is valid.
    /// </summary>
    /// <param name="value">The options to check.</param>
    /// <returns><see langword="true"/> if the options are valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown when called on an obsolete class.</exception>
    [Obsolete("Use options.IsValid() instead.")]
    public static bool IsValid(this SlowQueryLogOptions value)
    {
        try
        {
            value.Validate();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Validates the specified options instance and throws an <see cref="ArgumentException"/> if it is invalid.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the options are invalid, containing a list of problems.</exception>
    /// <exception cref="InvalidOperationException">Thrown when called on an obsolete class.</exception>
    [Obsolete("Use options.EnsureValid() instead.")]
    public static void EnsureValid(this SlowQueryLogOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);
        value.Validate();
    }
}
