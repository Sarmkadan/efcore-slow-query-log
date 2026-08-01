using System;
using System.Collections.Generic;
using Xunit;
using EfCore.SlowQueryLog;

namespace EfCore.SlowQueryLog.Tests
{
    public class SlowQuerySampleJsonExtensionsTests
    {
        private static SlowQuerySample CreateSample()
        {
            return new SlowQuerySample
            {
                Sql = "SELECT * FROM table",
                Duration = TimeSpan.FromSeconds(5),
                CapturedAt = new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc),
                Parameters = "@param1=value1",
                Suggestions = new List<IndexSuggestion>
                {
                    new IndexSuggestion("table", new[] { "column" }, "reason")
                },
                Tags = new List<string> { "tag1", "tag2" }
            };
        }

        private static string ExpectedJson => 
            "{\"sql\":\"SELECT * FROM table\",\"duration\":\"00:00:05\",\"capturedAt\":\"2024-01-01T12:00:00Z\",\"parameters\":\"@param1=value1\",\"suggestions\":[{\"table\":\"table\",\"columns\":[\"column\"],\"reason\":\"reason\"}],\"tags\":[\"tag1\",\"tag2\"]}";

        [Fact]
        public void ToJson_WithValidSample_ReturnsJsonString()
        {
            // Arrange
            var sample = CreateSample();

            // Act
            var json = sample.ToJson();

            // Assert
            Assert.NotNull(json);
            Assert.Equal(ExpectedJson, json);
        }

        [Fact]
        public void ToJson_WithIndentation_ReturnsFormattedJson()
        {
            // Arrange
            var sample = CreateSample();

            // Act
            var json = sample.ToJson(indented: true);

            // Assert
            Assert.NotNull(json);
            // Indented JSON contains line breaks; we just verify that it is not the same as the compact version
            Assert.NotEqual(ExpectedJson, json);
            Assert.Contains("\n", json);
        }

        [Fact]
        public void ToJson_NullSample_ThrowsArgumentNullException()
        {
            // Arrange
            SlowQuerySample? sample = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => sample!.ToJson());
        }

        [Fact]
        public void FromJson_WithValidJson_ReturnsSample()
        {
            // Arrange
            var json = ExpectedJson;

            // Act
            var sample = SlowQuerySampleJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(sample);
            Assert.Equal("SELECT * FROM table", sample!.Sql);
            Assert.Equal(TimeSpan.FromSeconds(5), sample.Duration);
            Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), sample.CapturedAt);
            Assert.Equal("@param1=value1", sample.Parameters);
            Assert.Single(sample.Suggestions);
            Assert.Equal("table", sample.Suggestions[0].Table);
            Assert.Equal("column", sample.Suggestions[0].Columns[0]);
            Assert.Equal("reason", sample.Suggestions[0].Reason);
            Assert.Equal(2, sample.Tags.Count);
            Assert.Contains("tag1", sample.Tags);
            Assert.Contains("tag2", sample.Tags);
        }

        [Fact]
        public void FromJson_NullInput_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SlowQuerySampleJsonExtensions.FromJson(null!));
        }

        [Fact]
        public void FromJson_EmptyString_ReturnsNull()
        {
            // Act
            var result = SlowQuerySampleJsonExtensions.FromJson(string.Empty);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void TryFromJson_WithValidJson_ReturnsTrueAndSample()
        {
            // Arrange
            var json = ExpectedJson;

            // Act
            var success = SlowQuerySampleJsonExtensions.TryFromJson(json, out var sample);

            // Assert
            Assert.True(success);
            Assert.NotNull(sample);
            Assert.Equal("SELECT * FROM table", sample!.Sql);
            Assert.Equal(TimeSpan.FromSeconds(5), sample.Duration);
            Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0, DateTimeKind.Utc), sample.CapturedAt);
            Assert.Equal("@param1=value1", sample.Parameters);
            Assert.Single(sample.Suggestions);
            Assert.Equal("table", sample.Suggestions[0].Table);
            Assert.Equal("column", sample.Suggestions[0].Columns[0]);
            Assert.Equal("reason", sample.Suggestions[0].Reason);
            Assert.Equal(2, sample.Tags.Count);
        }

        [Fact]
        public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
        {
            // Act
            var success = SlowQuerySampleJsonExtensions.TryFromJson("invalid json", out var sample);

            // Assert
            Assert.False(success);
            Assert.Null(sample);
        }

        [Fact]
        public void TryFromJson_NullOrEmpty_ReturnsFalseAndNull()
        {
            // Null input
            var successNull = SlowQuerySampleJsonExtensions.TryFromJson(null!, out var sampleNull);
            Assert.False(successNull);
            Assert.Null(sampleNull);

            // Empty string
            var successEmpty = SlowQuerySampleJsonExtensions.TryFromJson(string.Empty, out var sampleEmpty);
            Assert.False(successEmpty);
            Assert.Null(sampleEmpty);
        }
    }
}
