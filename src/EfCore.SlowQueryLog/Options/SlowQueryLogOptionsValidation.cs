using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace EfCore.SlowQueryLog.Options;

/// <summary>
/// Provides validation methods for <see cref="SlowQueryLogOptions"/> instances.
/// </summary>
public static class SlowQueryLogOptionsValidation
{
    /// <summary>
    /// Validates the specified options instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <returns>An empty list if the options are valid; otherwise, a list of error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this SlowQueryLogOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.Threshold <= TimeSpan.Zero)
        {
            errors.Add($"The {nameof(SlowQueryLogOptions.Threshold)} must be positive, but was {value.Threshold}.");
        }

        if (value.RankingCapacity <= 0)
        {
            errors.Add($"The {nameof(SlowQueryLogOptions.RankingCapacity)} must be positive, but was {value.RankingCapacity}.");
        }

        return errors;
    }

    /// <summary>
    /// Determines whether the specified options instance is valid.
    /// </summary>
    /// <param name="value">The options to check.</param>
    /// <returns><see langword="true"/> if the options are valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static bool IsValid(this SlowQueryLogOptions value)
    {
        return value is not null
            && value.Threshold > TimeSpan.Zero
            && value.RankingCapacity > 0;
    }

    /// <summary>
    /// Validates the specified options instance and throws an <see cref="ArgumentException"/> if it is invalid.
    /// </summary>
    /// <param name="value">The options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when the options are invalid, containing a list of problems.</exception>
    public static void EnsureValid(this SlowQueryLogOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (value.Threshold <= TimeSpan.Zero)
        {
            errors.Add($"The {nameof(SlowQueryLogOptions.Threshold)} must be positive, but was {value.Threshold}.");
        }

        if (value.RankingCapacity <= 0)
        {
            errors.Add($"The {nameof(SlowQueryLogOptions.RankingCapacity)} must be positive, but was {value.RankingCapacity}.");
        }

        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"The {nameof(SlowQueryLogOptions)} instance is invalid. Problems:\n{string.Join(Environment.NewLine, errors)}",
                nameof(value));
        }
    }
}