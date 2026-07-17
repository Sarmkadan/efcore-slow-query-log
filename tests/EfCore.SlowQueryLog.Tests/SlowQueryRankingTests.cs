using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Reporting;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Tests for the SlowQueryRanking class.
/// </summary>
public class SlowQueryRankingTests
{
    /// <summary>
    /// Creates a new SlowQuerySample instance with the specified duration.
    /// </summary>
    /// <param name="ms">The duration in milliseconds.</param>
    /// <returns>A new SlowQuerySample instance.</returns>
    private static SlowQuerySample Sample(int ms) => new()
    {
        Sql = $"SELECT {ms}",
        Duration = TimeSpan.FromMilliseconds(ms),
        CapturedAt = DateTimeOffset.UtcNow,
    };

    /// <summary>
    /// Verifies that the SlowQueryRanking orders the samples by duration in descending order.
    /// </summary>
    [Fact]
    public void Orders_by_duration_descending()
    {
        var ranking = new SlowQueryRanking(10);
        ranking.Add(Sample(100));
        ranking.Add(Sample(900));
        ranking.Add(Sample(500));

        var snap = ranking.Snapshot();

        Assert.Equal(900, snap[0].Duration.TotalMilliseconds);
        Assert.Equal(500, snap[1].Duration.TotalMilliseconds);
        Assert.Equal(100, snap[2].Duration.TotalMilliseconds);
    }

    /// <summary>
    /// Verifies that the SlowQueryRanking respects the capacity and keeps the slowest samples.
    /// </summary>
    [Fact]
    public void Respects_capacity_keeping_slowest()
    {
        var ranking = new SlowQueryRanking(2);
        ranking.Add(Sample(100));
        ranking.Add(Sample(900));
        ranking.Add(Sample(500));

        var snap = ranking.Snapshot();

        Assert.Equal(2, snap.Count);
        Assert.Equal(900, snap[0].Duration.TotalMilliseconds);
        Assert.Equal(500, snap[1].Duration.TotalMilliseconds);
    }

    /// <summary>
    /// Verifies that creating a SlowQueryRanking with a capacity of 0 throws an ArgumentOutOfRangeException.
    /// </summary>
    [Fact]
    public void Zero_capacity_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlowQueryRanking(0));
    }
}
