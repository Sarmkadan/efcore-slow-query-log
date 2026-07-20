using System.Text.Json;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Tests for <see cref="SlowQuerySampleJsonExtensions"/> serialization/deserialization.
/// </summary>
public sealed class SlowQuerySampleJsonTests
{
    [Fact]
    public void ToJson_SerializesAllFieldsCorrectly()
    {
        // Arrange
        var sample = new SlowQuerySample
        {
            Sql = "SELECT * FROM Users WHERE Id = @id",
            Duration = TimeSpan.FromSeconds(5),
            CapturedAt = DateTimeOffset.Parse("2024-01-15T10:30:00+00:00"),
            Parameters = "@id=123",
            Suggestions = new[] { new IndexSuggestion("Users", new[] { "Id" }, "filtered in WHERE") }
        };

        // Act
        var json = sample.ToJson();

        // Assert - verify all fields are present (using camelCase property names)
        Assert.Contains("\"sql\":\"SELECT * FROM Users WHERE Id = @id\"", json);
        Assert.Contains("\"duration\":", json);
        Assert.Contains("\"capturedAt\":\"2024-01-15T10:30:00+00:00\"", json);
        Assert.Contains("\"parameters\":\"@id=123\"", json);
        Assert.Contains("\"suggestions\"", json);
        Assert.Contains("\"Users\"", json);
        Assert.Contains("\"Id\"", json);
    }

    [Fact]
    public void ToJson_WithIndentedFormat_ProducesFormattedJson()
    {
        // Arrange
        var sample = new SlowQuerySample
        {
            Sql = "SELECT * FROM Users",
            Duration = TimeSpan.FromSeconds(1),
            CapturedAt = DateTimeOffset.Parse("2024-01-01T00:00:00+00:00"),
            Suggestions = Array.Empty<IndexSuggestion>()
        };

        // Act
        var json = sample.ToJson(indented: true);

        // Assert - should contain newlines and indentation
        Assert.Contains("\n", json);
        Assert.Contains("  ", json); // 2-space indentation
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);
    }

    [Fact]
    public void ToJson_WithoutIndentedFormat_ProducesCompactJson()
    {
        // Arrange
        var sample = new SlowQuerySample
        {
            Sql = "SELECT * FROM Users",
            Duration = TimeSpan.FromSeconds(1),
            CapturedAt = DateTimeOffset.Parse("2024-01-01T00:00:00+00:00"),
            Suggestions = Array.Empty<IndexSuggestion>()
        };

        // Act
        var json = sample.ToJson(indented: false);

        // Assert - should be compact without extra whitespace
        Assert.DoesNotContain("\n", json);
        Assert.DoesNotContain("  ", json);
    }

    [Fact]
    public void ToJson_WithNullParameters_SerializesAsNull()
    {
        // Arrange
        var sample = new SlowQuerySample
        {
            Sql = "SELECT * FROM Users",
            Duration = TimeSpan.FromSeconds(1),
            CapturedAt = DateTimeOffset.Parse("2024-01-01T00:00:00+00:00"),
            Parameters = null,
            Suggestions = Array.Empty<IndexSuggestion>()
        };

        // Act
        var json = sample.ToJson();

        // Assert - parameters should be serialized as null
        Assert.Contains("\"parameters\":null", json);
    }

    [Fact]
    public void ToJson_WithEmptySuggestions_SerializesAsEmptyArray()
    {
        // Arrange
        var sample = new SlowQuerySample
        {
            Sql = "SELECT * FROM Users",
            Duration = TimeSpan.FromSeconds(1),
            CapturedAt = DateTimeOffset.Parse("2024-01-01T00:00:00+00:00"),
            Suggestions = Array.Empty<IndexSuggestion>()
        };

        // Act
        var json = sample.ToJson();

        // Assert - suggestions should be serialized as empty array
        Assert.Contains("\"suggestions\":[]", json);
    }

    [Fact]
    public void FromJson_DeserializesAllFieldsCorrectly()
    {
        // Arrange - use roundtrip to avoid TimeSpan format issues
        var original = new SlowQuerySample
        {
            Sql = "SELECT * FROM Orders WHERE CustomerId = @customerId",
            Duration = TimeSpan.FromSeconds(2),
            CapturedAt = DateTimeOffset.Parse("2024-02-20T14:45:30+03:00"),
            Parameters = "@customerId=456",
            Suggestions = new[] { new IndexSuggestion("Orders", new[] { "CustomerId" }, "filtered in WHERE") }
        };

        var json = original.ToJson();

        // Act
        var result = SlowQuerySampleJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(original.Sql, result.Sql);
        Assert.Equal(original.Duration, result.Duration);
        Assert.Equal(original.CapturedAt, result.CapturedAt);
        Assert.Equal(original.Parameters, result.Parameters);
        Assert.NotNull(result.Suggestions);
        Assert.Single(result.Suggestions);
        Assert.Equal(original.Suggestions[0].Table, result.Suggestions[0].Table);
        Assert.Equal(original.Suggestions[0].Columns, result.Suggestions[0].Columns);
        Assert.Equal(original.Suggestions[0].Reason, result.Suggestions[0].Reason);
    }

    [Fact]
    public void FromJson_WithNullParameters_DeserializesCorrectly()
    {
        // Arrange - use roundtrip
        var original = new SlowQuerySample
        {
            Sql = "SELECT * FROM Users",
            Duration = TimeSpan.FromSeconds(1),
            CapturedAt = DateTimeOffset.Parse("2024-01-01T00:00:00+00:00"),
            Parameters = null,
            Suggestions = Array.Empty<IndexSuggestion>()
        };

        var json = original.ToJson();

        // Act
        var result = SlowQuerySampleJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Parameters);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public void FromJson_WithEmptySuggestions_DeserializesCorrectly()
    {
        // Arrange - use roundtrip
        var original = new SlowQuerySample
        {
            Sql = "SELECT * FROM Users",
            Duration = TimeSpan.FromSeconds(1),
            CapturedAt = DateTimeOffset.Parse("2024-01-01T00:00:00+00:00"),
            Suggestions = Array.Empty<IndexSuggestion>()
        };

        var json = original.ToJson();

        // Act
        var result = SlowQuerySampleJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result.Suggestions);
    }

    [Fact]
    public void FromJson_RoundtripPreservesAllFields()
    {
        // Arrange - create a complex sample with all fields
        var original = new SlowQuerySample
        {
            Sql = "SELECT u.Id, u.Name, o.Total FROM Users u JOIN Orders o ON u.Id = o.UserId WHERE u.Active = 1 ORDER BY o.Total DESC",
            Duration = TimeSpan.FromMilliseconds(1250),
            CapturedAt = DateTimeOffset.Parse("2024-03-10T09:15:22+00:00"),
            Parameters = "@active=1",
            Suggestions = new[]
            {
                new IndexSuggestion("Users", new[] { "Active" }, "Active (filtered in WHERE)"),
                new IndexSuggestion("Orders", new[] { "UserId" }, "UserId (join key)"),
                new IndexSuggestion("Orders", new[] { "Total" }, "Total (sort column)")
            }
        };

        // Act - serialize and deserialize
        var json = original.ToJson();
        var deserialized = SlowQuerySampleJsonExtensions.FromJson(json);

        // Assert - all fields should be preserved
        Assert.NotNull(deserialized);
        Assert.Equal(original.Sql, deserialized.Sql);
        Assert.Equal(original.Duration, deserialized.Duration);
        Assert.Equal(original.CapturedAt, deserialized.CapturedAt);
        Assert.Equal(original.Parameters, deserialized.Parameters);
        Assert.Equal(original.Suggestions.Count, deserialized.Suggestions.Count);

        for (int i = 0; i < original.Suggestions.Count; i++)
        {
            Assert.Equal(original.Suggestions[i].Table, deserialized.Suggestions[i].Table);
            Assert.Equal(original.Suggestions[i].Columns, deserialized.Suggestions[i].Columns);
            Assert.Equal(original.Suggestions[i].Reason, deserialized.Suggestions[i].Reason);
        }
    }

    [Fact]
    public void FromJson_SpecialCharactersInSql_SurviveRoundtrip()
    {
        // Arrange - SQL with special characters that need escaping
        var specialSql = "SELECT * FROM \"Users With Spaces\" WHERE Name = 'O\"Brien' AND Email LIKE '%@test.com%'";
        var original = new SlowQuerySample
        {
            Sql = specialSql,
            Duration = TimeSpan.FromSeconds(1),
            CapturedAt = DateTimeOffset.Parse("2024-01-01T00:00:00+00:00"),
            Suggestions = Array.Empty<IndexSuggestion>()
        };

        // Act - serialize and deserialize
        var json = original.ToJson();
        var deserialized = SlowQuerySampleJsonExtensions.FromJson(json);

        // Assert - special characters should be preserved
        Assert.NotNull(deserialized);
        Assert.Equal(specialSql, deserialized.Sql);
    }

    [Fact]
    public void FromJson_NewlinesAndTabsInSql_SurviveRoundtrip()
    {
        // Arrange - SQL with newlines and tabs
        var sqlWithWhitespace = "SELECT *\nFROM Users\tWHERE Id = 1";
        var original = new SlowQuerySample
        {
            Sql = sqlWithWhitespace,
            Duration = TimeSpan.FromSeconds(1),
            CapturedAt = DateTimeOffset.Parse("2024-01-01T00:00:00+00:00"),
            Suggestions = Array.Empty<IndexSuggestion>()
        };

        // Act - serialize and deserialize
        var json = original.ToJson();
        var deserialized = SlowQuerySampleJsonExtensions.FromJson(json);

        // Assert - whitespace should be preserved
        Assert.NotNull(deserialized);
        Assert.Equal(sqlWithWhitespace, deserialized.Sql);
    }

    [Fact]
    public void FromJson_UnicodeCharactersInSql_SurviveRoundtrip()
    {
        // Arrange - SQL with Unicode characters
        var unicodeSql = "SELECT * FROM Users WHERE Name = 'José' AND City = 'München'";
        var original = new SlowQuerySample
        {
            Sql = unicodeSql,
            Duration = TimeSpan.FromSeconds(1),
            CapturedAt = DateTimeOffset.Parse("2024-01-01T00:00:00+00:00"),
            Suggestions = Array.Empty<IndexSuggestion>()
        };

        // Act - serialize and deserialize
        var json = original.ToJson();
        var deserialized = SlowQuerySampleJsonExtensions.FromJson(json);

        // Assert - Unicode should be preserved
        Assert.NotNull(deserialized);
        Assert.Equal(unicodeSql, deserialized.Sql);
    }

    [Fact]
    public void FromJson_NullJson_ThrowsArgumentNullException()
    {
        // Arrange
        string json = null!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => SlowQuerySampleJsonExtensions.FromJson(json));
    }

    [Fact]
    public void FromJson_EmptyJson_ReturnsNull()
    {
        // Arrange
        var json = "";

        // Act
        var result = SlowQuerySampleJsonExtensions.FromJson(json);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_WhitespaceJson_ThrowsJsonException()
    {
        // Arrange
        var json = "   \n\t  ";

        // Act & Assert
        Assert.Throws<JsonException>(() => SlowQuerySampleJsonExtensions.FromJson(json));
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "{ this is not valid json";

        // Act & Assert
        Assert.Throws<JsonException>(() => SlowQuerySampleJsonExtensions.FromJson(json));
    }

    [Fact]
    public void FromJson_InvalidJsonStructure_ThrowsJsonException()
    {
        // Arrange - valid JSON but wrong structure
        var json = "{\"notSql\":\"test\"}";

        // Act & Assert
        Assert.Throws<JsonException>(() => SlowQuerySampleJsonExtensions.FromJson(json));
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndDeserializes()
    {
        // Arrange - use roundtrip
        var original = new SlowQuerySample
        {
            Sql = "SELECT * FROM Users",
            Duration = TimeSpan.FromSeconds(1),
            CapturedAt = DateTimeOffset.Parse("2024-01-01T00:00:00+00:00"),
            Suggestions = Array.Empty<IndexSuggestion>()
        };

        var json = original.ToJson();

        // Act
        var result = SlowQuerySampleJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.True(result);
        Assert.NotNull(value);
        Assert.Equal("SELECT * FROM Users", value.Sql);
    }

    [Fact]
    public void TryFromJson_NullJson_ReturnsFalseAndNull()
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
    public void TryFromJson_EmptyJson_ReturnsFalseAndNull()
    {
        // Arrange
        var json = "";

        // Act
        var result = SlowQuerySampleJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_WhitespaceJson_ReturnsFalseAndNull()
    {
        // Arrange
        var json = "   \n\t  ";

        // Act
        var result = SlowQuerySampleJsonExtensions.TryFromJson(json, out var value);

        // Assert - whitespace-only strings are not caught by string.IsNullOrEmpty, so TryFromJson also returns false
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        // Arrange
        var json = "invalid json { not valid";

        // Act
        var result = SlowQuerySampleJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }

    [Fact]
    public void TryFromJson_InvalidJsonStructure_ReturnsFalseAndNull()
    {
        // Arrange - valid JSON but wrong structure
        var json = "{\"wrongField\":\"value\"}";

        // Act
        var result = SlowQuerySampleJsonExtensions.TryFromJson(json, out var value);

        // Assert
        Assert.False(result);
        Assert.Null(value);
    }
}