using Xunit;
using EfCore.SlowQueryLog;
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace EfCore.SlowQueryLog.Tests
{
    public class SlowQuerySampleJsonExtensionsTests
    {
        [Fact]
        public void ToJson_HappyPath_SerializesAllFieldsCorrectly()
        {
            // Arrange
            var sample = new SlowQuerySample
            {
                Sql = "SELECT * FROM table",
                Duration = TimeSpan.FromSeconds(5),
                CapturedAt = new DateTime(2024, 1, 1, 12, 0, 0),
                Parameters = "@param1=value1",
                Suggestions = new List<IndexSuggestion>
                {
                    new IndexSuggestion("table", new[] { "column" }, "reason")
                },
                Tags = new List<string> { "tag1", "tag2" }
            };

            // Act
            var json = SlowQuerySampleJsonExtensions.ToJson(sample);

            // Assert
            Assert.NotNull(json);
            Assert.Contains("sql", json);
            Assert.Contains("duration", json);
            Assert.Contains("capturedAt", json);
            Assert.Contains("parameters", json);
            Assert.Contains("suggestions", json);
            Assert.Contains("tags", json);
        }

        [Fact]
        public void FromJson_HappyPath_DeserializesAllFieldsCorrectly()
        {
            // Arrange
            var json = "{\"sql\":\"SELECT * FROM table\",\"duration\":\"00:00:05\",\"capturedAt\":\"2024-01-01T12:00:00\",\"parameters\":\"@param1=value1\",\"suggestions\":[{\"table\":\"table\",\"columns\":[\"column\"],\"reason\":\"reason\"}],\"tags\":[\"tag1\",\"tag2\"]}";

            // Act
            var sample = SlowQuerySampleJsonExtensions.FromJson(json);

            // Assert
            Assert.NotNull(sample);
            Assert.Equal("SELECT * FROM table", sample.Sql);
            Assert.Equal(TimeSpan.FromSeconds(5), sample.Duration);
            Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), sample.CapturedAt);
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
        public void TryFromJson_HappyPath_DeserializesAllFieldsCorrectly()
        {
            // Arrange
            var json = "{\"sql\":\"SELECT * FROM table\",\"duration\":\"00:00:05\",\"capturedAt\":\"2024-01-01T12:00:00\",\"parameters\":\"@param1=value1\",\"suggestions\":[{\"table\":\"table\",\"columns\":[\"column\"],\"reason\":\"reason\"}],\"tags\":[\"tag1\",\"tag2\"]}";

            // Act
            var success = SlowQuerySampleJsonExtensions.TryFromJson(json, out var sample);

            // Assert
            Assert.True(success);
            Assert.NotNull(sample);
            Assert.Equal("SELECT * FROM table", sample.Sql);
            Assert.Equal(TimeSpan.FromSeconds(5), sample.Duration);
            Assert.Equal(new DateTime(2024, 1, 1, 12, 0, 0), sample.CapturedAt);
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
        public void ToJson_ThrowsArgumentNullException_ForNullInput()
        {
            // Arrange
            SlowQuerySample? nullSample = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => nullSample!.ToJson());
        }

        [Fact]
        public void FromJson_ThrowsArgumentNullException_ForNullInput()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => SlowQuerySampleJsonExtensions.FromJson(null!));
        }

        [Fact]
        public void FromJson_ReturnsNull_ForEmptyString()
        {
            // Act
            var result = SlowQuerySampleJsonExtensions.FromJson("");

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public void TryFromJson_ReturnsFalseAndNull_ForNullInput()
        {
            // Arrange
            string json = null!;

            // Act
            var result = SlowQuerySampleJsonExtensions.TryFromJson(json, out var value);

            // Assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryFromJson_ReturnsFalseAndNull_ForEmptyString()
        {
            // Act
            var result = SlowQuerySampleJsonExtensions.TryFromJson("", out var value);

            // Assert
            Assert.False(result);
            Assert.Null(value);
        }

        [Fact]
        public void TryFromJson_ReturnsFalseAndNull_ForInvalidJson()
        {
            // Act
            var result = SlowQuerySampleJsonExtensions.TryFromJson("invalid json", out var value);

            // Assert
            Assert.False(result);
            Assert.Null(value);
        }
    }
}