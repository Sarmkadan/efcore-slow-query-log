using System;
using System.Collections.Generic;
using System.IO;
using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Reporting;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

public class SlowQueryRankingExtensionsTests
{
    private static SlowQuerySample Sample(int ms, string sql = "SELECT 1") => new()
    {
        Sql = sql,
        Duration = TimeSpan.FromMilliseconds(ms),
        CapturedAt = DateTimeOffset.UtcNow,
    };

    [Fact]
    public void GetTotalDuration_ReturnsCorrectDuration()
    {
        var ranking = new SlowQueryRanking(10);
        ranking.Add(Sample(100));
        ranking.Add(Sample(200));
        
        Assert.Equal(TimeSpan.FromMilliseconds(300), ranking.GetTotalDuration());
    }

    [Fact]
    public void GetTotalDuration_EmptyRanking_ReturnsZero()
    {
        var ranking = new SlowQueryRanking(10);
        Assert.Equal(TimeSpan.Zero, ranking.GetTotalDuration());
    }

    [Fact]
    public void GetAverageDuration_ReturnsCorrectAverage()
    {
        var ranking = new SlowQueryRanking(10);
        ranking.Add(Sample(100));
        ranking.Add(Sample(200));
        
        Assert.Equal(150.0, ranking.GetAverageDuration());
    }

    [Fact]
    public void GetFingerprints_OrdersByAverageDurationDescending()
    {
        var ranking = new SlowQueryRanking(10);
        ranking.Add(Sample(100, "Query1")); // Avg: 100
        ranking.Add(Sample(300, "Query2")); // Avg: 300
        
        var fingerprints = ranking.GetFingerprints();
        Assert.Equal("Query2", fingerprints[0].Sql);
        Assert.Equal("Query1", fingerprints[1].Sql);
    }

    [Fact]
    public void GetFingerprintsByTotalDuration_OrdersCorrectly()
    {
        var ranking = new SlowQueryRanking(10);
        ranking.Add(Sample(100, "Query1")); // Total: 100
        ranking.Add(Sample(300, "Query2")); // Total: 300
        
        var fingerprints = ranking.GetFingerprintsByTotalDuration();
        Assert.Equal("Query2", fingerprints[0].Sql);
        Assert.Equal("Query1", fingerprints[1].Sql);
    }

    [Fact]
    public void ThrowsArgumentNullException_WhenRankingIsNull()
    {
        Assert.Throws<ArgumentNullException>(() => SlowQueryRankingExtensions.GetTotalDuration(null!));
        Assert.Throws<ArgumentNullException>(() => SlowQueryRankingExtensions.GetAverageDuration(null!));
        Assert.Throws<ArgumentNullException>(() => SlowQueryRankingExtensions.GetAllSuggestions(null!));
        Assert.Throws<ArgumentNullException>(() => SlowQueryRankingExtensions.GetFingerprints(null!));
        Assert.Throws<ArgumentNullException>(() => SlowQueryRankingExtensions.GetFingerprintsByTotalDuration(null!));
        Assert.Throws<ArgumentNullException>(() => SlowQueryRankingExtensions.GetFingerprintsByP95Duration(null!));
        Assert.Throws<ArgumentNullException>(() => SlowQueryRankingExtensions.GetFingerprintsByMaxDuration(null!));
        Assert.Throws<ArgumentNullException>(() => SlowQueryRankingExtensions.ExportToJson(null!, "file.json"));
        Assert.Throws<ArgumentNullException>(() => SlowQueryRankingExtensions.GenerateMarkdownReport(null!));
        Assert.Throws<ArgumentNullException>(() => SlowQueryRankingExtensions.WriteMarkdownReport(null!, "file.md"));
    }
}
