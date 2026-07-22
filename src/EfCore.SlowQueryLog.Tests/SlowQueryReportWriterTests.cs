using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Reporting;
using EfCore.SlowQueryLog;
using Xunit;

namespace EfCore.SlowQueryLog.Tests
{
    /// <summary>
    /// Tests for <see cref="SlowQueryReportWriter"/>.
    /// </summary>
    public class SlowQueryReportWriterTests
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        [Fact]
        public void WriteReport_NullFilePath_ThrowsArgumentNullException()
        {
            var ranking = new SlowQueryRanking(10, 10);
            Assert.Throws<ArgumentNullException>(() => SlowQueryReportWriter.WriteReport(null!, ranking));
        }

        [Fact]
        public void WriteReport_NullRanking_ThrowsArgumentNullException()
        {
            var tempFile = Path.GetTempFileName();
            try
            {
                Assert.Throws<ArgumentNullException>(() => SlowQueryReportWriter.WriteReport(tempFile, null!));
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void WriteReport_EmptyRanking_WritesEmptyCollections()
        {
            var ranking = new SlowQueryRanking(10, 10);
            var tempFile = Path.GetTempFileName();

            try
            {
                SlowQueryReportWriter.WriteReport(tempFile, ranking, indented: true);

                var json = File.ReadAllText(tempFile);
                var report = JsonSerializer.Deserialize<ReportDto>(json, JsonOptions)!;

                Assert.NotNull(report.GeneratedAt);
                Assert.Empty(report.Samples);
                Assert.Empty(report.Fingerprints);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void WriteReport_HappyPath_WritesCorrectData()
        {
            // Arrange: create a ranking with a sample and a fingerprint
            var ranking = new SlowQueryRanking(maxSamples: 10, capacity: 10);
            var sample = new SlowQuerySample
            {
                Sql = "SELECT 1",
                Duration = TimeSpan.FromMilliseconds(123),
                CapturedAt = DateTimeOffset.UtcNow,
                Parameters = "p=1",
                Suggestions = Array.Empty<IndexSuggestion>()
            };
            ranking.Add(sample);

            // Force fingerprint generation by accessing it
            var fingerprints = ranking.GetFingerprints();

            var tempFile = Path.GetTempFileName();

            try
            {
                // Act
                SlowQueryReportWriter.WriteReport(tempFile, ranking, indented: false);

                // Assert
                var json = File.ReadAllText(tempFile);
                var report = JsonSerializer.Deserialize<ReportDto>(json, JsonOptions)!;

                // GeneratedAt should be recent (within a minute)
                Assert.InRange(report.GeneratedAt, DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddMinutes(1));

                // Samples
                Assert.Single(report.Samples);
                var deserializedSample = report.Samples[0];
                Assert.Equal(sample.Sql, deserializedSample.Sql);
                Assert.Equal(sample.Duration, deserializedSample.Duration);
                Assert.Equal(sample.Parameters, deserializedSample.Parameters);
                Assert.Equal(sample.Suggestions?.Length ?? 0, deserializedSample.Suggestions?.Length ?? 0);

                // Fingerprints
                Assert.Equal(fingerprints.Count, report.Fingerprints?.Count ?? 0);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        [Fact]
        public void WriteReport_Indented_ProducesMultilineJson()
        {
            var ranking = new SlowQueryRanking(10, 10);
            var tempFile = Path.GetTempFileName();

            try
            {
                SlowQueryReportWriter.WriteReport(tempFile, ranking, indented: true);
                var json = File.ReadAllText(tempFile);

                // Indented JSON contains line breaks after opening brace
                Assert.Contains("\n", json);
                Assert.Contains(Environment.NewLine, json);
            }
            finally
            {
                File.Delete(tempFile);
            }
        }

        // Helper DTO matching the private ReportDto shape for deserialization
        private sealed class ReportDto
        {
            public DateTimeOffset GeneratedAt { get; set; }
            public IReadOnlyList<SlowQuerySample> Samples { get; set; } = Array.Empty<SlowQuerySample>();
            public IReadOnlyList<SlowQueryFingerprint> Fingerprints { get; set; } = Array.Empty<SlowQueryFingerprint>();
        }
    }
}
