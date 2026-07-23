using System;
using EfCore.SlowQueryLog.Options;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Constructs the <see cref="ISlowQueryRanking"/> implementation selected by
/// <see cref="SlowQueryLogOptions.RankingMode"/>.
/// </summary>
public static class SlowQueryRankingFactory
{
    /// <summary>
    /// Creates an <see cref="ISlowQueryRanking"/> instance matching <paramref name="options"/>.RankingMode:
    /// <see cref="SlowQueryRankingMode.Exact"/> yields a <see cref="SlowQueryRanking"/>,
    /// <see cref="SlowQueryRankingMode.Fingerprint"/> yields a <see cref="SlowQueryFingerprintRanking"/>.
    /// </summary>
    /// <param name="options">The options describing capacity and ranking mode.</param>
    /// <returns>A new <see cref="ISlowQueryRanking"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="options"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="options"/>.RankingMode is not a recognized value.</exception>
    public static ISlowQueryRanking Create(SlowQueryLogOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.RankingMode switch
        {
            SlowQueryRankingMode.Exact => new SlowQueryRanking(options.MaxSamples, options.RankingCapacity),
            SlowQueryRankingMode.Fingerprint => new SlowQueryFingerprintRanking(options.RankingCapacity),
            _ => throw new ArgumentOutOfRangeException(nameof(options), options.RankingMode, "Unrecognized SlowQueryRankingMode value.")
        };
    }
}
