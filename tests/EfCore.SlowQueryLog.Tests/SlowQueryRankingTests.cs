using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Reporting;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

public class SlowQueryRankingTests
{
    private static SlowQuerySample Sample(int ms) => new()
    {
        Sql = $"SELECT {ms}",
        Duration = TimeSpan.FromMilliseconds(ms),
        CapturedAt = DateTimeOffset.UtcNow,
    };

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

    [Fact]
    public void Zero_capacity_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SlowQueryRanking(0));
    }
}
