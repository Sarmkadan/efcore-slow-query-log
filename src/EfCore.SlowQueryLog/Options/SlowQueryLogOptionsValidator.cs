using System;
using System.Collections.Generic;
using Microsoft.Extensions.Options;
using EfCore.SlowQueryLog.Options;

namespace EfCore.SlowQueryLog.Options;

/// <summary>
/// Validates <see cref="SlowQueryLogOptions"/> values using the <see cref="IValidateOptions{TOptions}"/> pattern.
/// </summary>
public sealed class SlowQueryLogOptionsValidator : IValidateOptions<SlowQueryLogOptions>
{
    /// <inheritdoc />
    public ValidateOptionsResult Validate(string? name, SlowQueryLogOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("Options instance is null.");
        }

        var failures = new List<string>();

        // Threshold must be positive
        if (options.Threshold <= TimeSpan.Zero)
        {
            failures.Add("Threshold must be positive.");
        }

        // RankingCapacity must be positive
        if (options.RankingCapacity <= 0)
        {
            failures.Add("RankingCapacity must be positive.");
        }

        // MaxSamples must be positive
        if (options.MaxSamples <= 0)
        {
            failures.Add("MaxSamples must be positive.");
        }

        // SamplingRate must be between 0.0 and 1.0 (inclusive)
        if (options.SamplingRate < 0.0 || options.SamplingRate > 1.0)
        {
            failures.Add("SamplingRate must be between 0.0 and 1.0.");
        }

        // MaxAnalysesPerMinute must be non‑negative
        if (options.MaxAnalysesPerMinute < 0)
        {
            failures.Add("MaxAnalysesPerMinute must be non-negative.");
        }

        // BackgroundQueueCapacity must be positive
        if (options.BackgroundQueueCapacity <= 0)
        {
            failures.Add("BackgroundQueueCapacity must be positive.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
