using System.Text.Json;
using EfCore.SlowQueryLog;
using Xunit;

/// <summary>
/// Tests for SlowQuerySampleJsonExtensions serialization and deserialization methods.
/// </summary>
public class SlowQuerySampleJsonExtensionsTests
{
    private static readonly DateTimeOffset _testTime = new(2024, 1, 1, 12, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// Creates a sample SlowQuerySample for testing.
    /// </summary>
    private static SlowQuerySample CreateSample(
        string sql = "SELECT * FROM Users WHERE Status = @status",
        TimeSpan? duration = null,
        string? parameters = null,
        IReadOnlyList<IndexSuggestion>? suggestions = null)
    {
        return new SlowQuerySample
        {
            Sql = sql,
            Duration = duration ?? TimeSpan.FromSeconds(5),
            CapturedAt = _testTime,
            Parameters = parameters,
            Suggestions = suggestions ?? Array.Empty<IndexSuggestion>()
        };
    }

    /// <summary>
    /// Verifies that ToJson serializes a SlowQuerySample to JSON with camelCase property names.
    /// </summary>
    [Fact]
    public void ToJson_serializes_to_camelCase_json()
    {
        var sample = CreateSample();
        var json = sample.ToJson();

        Assert.Contains("sql", json);
        Assert.Contains("duration", json);
        Assert.Contains("capturedAt", json);
        Assert.Contains("parameters", json);
        Assert.Contains("suggestions", json);
        Assert.DoesNotContain("Sql", json);
        Assert.DoesNotContain("Duration", json);
    }

    /// <summary>
    /// Verifies that ToJson with indented=true produces formatted JSON.
    /// </summary>
    [Fact]
    public void ToJson_with_indented_true_produces_formatted_json()
    {
        var sample = CreateSample();
        var json = sample.ToJson(indented: true);

        // Should contain newlines and indentation
        Assert.Contains("\n", json);
        Assert.Contains("  ", json);

        // Should still be valid JSON
        var parsed = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.Equal(JsonValueKind.Object, parsed.ValueKind);
    }

    /// <summary>
    /// Verifies that ToJson with indented=false produces compact JSON.
    /// </summary>
    [Fact]
    public void ToJson_with_indented_false_produces_compact_json()
    {
        var sample = CreateSample();
        var json = sample.ToJson(indented: false);

        // Should not contain newlines
        Assert.DoesNotContain("\n", json);
        Assert.DoesNotContain("\r", json);
    }

    /// <summary>
    /// Verifies that ToJson throws ArgumentNullException for null input.
    /// </summary>
    [Fact]
    public void ToJson_throws_ArgumentNullException_for_null_input()
    {
        SlowQuerySample? nullSample = null;
        Assert.Throws<ArgumentNullException>(() => nullSample!.ToJson());
    }

    /// <summary>
    /// Verifies that FromJson deserializes valid JSON back to SlowQuerySample.
    /// </summary>
    [Fact]
    public void FromJson_deserializes_valid_json()
    {
        var original = CreateSample(
            sql: "SELECT u.* FROM Users u WHERE u.CreatedAt > @date",
            duration: TimeSpan.FromSeconds(2.5),
            parameters: "@date='2024-01-01'"
        );

        var json = original.ToJson();
        var deserialized = SlowQuerySampleJsonExtensions.FromJson(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Sql, deserialized.Sql);
        Assert.Equal(original.Duration, deserialized.Duration);
        Assert.Equal(original.CapturedAt, deserialized.CapturedAt);
        Assert.Equal(original.Parameters, deserialized.Parameters);
        Assert.Equal(original.Suggestions, deserialized.Suggestions);
    }

    /// <summary>
    /// Verifies that FromJson returns null for null input.
    /// </summary>
    [Fact]
    public void FromJson_returns_null_for_null_input()
    {
        Assert.Throws<ArgumentNullException>(() => SlowQuerySampleJsonExtensions.FromJson(null!));
    }

    /// <summary>
    /// Verifies that FromJson returns null for empty string input.
    /// </summary>
    [Fact]
    public void FromJson_returns_null_for_empty_string()
    {
        var result = SlowQuerySampleJsonExtensions.FromJson("");
        Assert.Null(result);
    }

    /// <summary>
    /// Verifies that FromJson throws JsonException for whitespace-only string input.
    /// </summary>
    [Fact]
    public void FromJson_throws_JsonException_for_whitespace_string()
    {
        Assert.Throws<JsonException>(() => SlowQuerySampleJsonExtensions.FromJson("   "));
    }

    /// <summary>
    /// Verifies that FromJson throws JsonException for invalid JSON.
    /// </summary>
    [Fact]
    public void FromJson_throws_JsonException_for_invalid_json()
    {
        Assert.Throws<JsonException>(() => SlowQuerySampleJsonExtensions.FromJson("invalid json"));
        Assert.Throws<JsonException>(() => SlowQuerySampleJsonExtensions.FromJson("{ malformed"));
    }

    /// <summary>
    /// Verifies that TryFromJson returns false and null for null input.
    /// </summary>
    [Fact]
    public void TryFromJson_returns_false_and_null_for_null_input()
    {
        var result = SlowQuerySampleJsonExtensions.TryFromJson(null, out var value);
        Assert.False(result);
        Assert.Null(value);
    }

    /// <summary>
    /// Verifies that TryFromJson returns false and null for empty string input.
    /// </summary>
    [Fact]
    public void TryFromJson_returns_false_and_null_for_empty_string()
    {
        var result = SlowQuerySampleJsonExtensions.TryFromJson("", out var value);
        Assert.False(result);
        Assert.Null(value);
    }

    /// <summary>
    /// Verifies that TryFromJson returns false and null for invalid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_returns_false_and_null_for_invalid_json()
    {
        var result = SlowQuerySampleJsonExtensions.TryFromJson("invalid json", out var value);
        Assert.False(result);
        Assert.Null(value);

        result = SlowQuerySampleJsonExtensions.TryFromJson("{ malformed", out value);
        Assert.False(result);
        Assert.Null(value);
    }

    /// <summary>
    /// Verifies that TryFromJson returns true and deserialized sample for valid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_returns_true_and_sample_for_valid_json()
    {
        var original = CreateSample(
            sql: "UPDATE Orders SET Status = @status WHERE Id = @id",
            duration: TimeSpan.FromMilliseconds(1500),
            parameters: "@status='completed', @id=42"
        );

        var json = original.ToJson();
        var result = SlowQuerySampleJsonExtensions.TryFromJson(json, out var deserialized);

        Assert.True(result);
        Assert.NotNull(deserialized);
        Assert.Equal(original.Sql, deserialized!.Sql);
        Assert.Equal(original.Duration, deserialized.Duration);
        Assert.Equal(original.CapturedAt, deserialized.CapturedAt);
        Assert.Equal(original.Parameters, deserialized.Parameters);
    }

    /// <summary>
    /// Verifies that serialization and deserialization are round-trip safe.
    /// </summary>
    [Fact]
    public void Round_trip_serialization_preserves_all_properties()
    {
        var original = CreateSample(
            sql: "SELECT COUNT(*) FROM Products WHERE Price > @minPrice AND CategoryId = @category",
            duration: TimeSpan.FromSeconds(1.234),
            parameters: "@minPrice=99.99, @category=5",
            suggestions: new List<IndexSuggestion>
            {
                new IndexSuggestion("Products", new[] { "Price", "CategoryId" }, "Filter on Price and CategoryId")
            }
        );

        var json = original.ToJson();
        var deserialized = SlowQuerySampleJsonExtensions.FromJson(json);

        Assert.NotNull(deserialized);
        Assert.Equal(original.Sql, deserialized.Sql);
        Assert.Equal(original.Duration, deserialized.Duration);
        Assert.Equal(original.CapturedAt, deserialized.CapturedAt);
        Assert.Equal(original.Parameters, deserialized.Parameters);
        Assert.Equal(original.Suggestions.Count, deserialized.Suggestions.Count);
        Assert.Equal(original.Suggestions[0].Table, deserialized.Suggestions[0].Table);
        Assert.Equal(original.Suggestions[0].Columns, deserialized.Suggestions[0].Columns);
        Assert.Equal(original.Suggestions[0].Reason, deserialized.Suggestions[0].Reason);
    }

    /// <summary>
    /// Verifies that samples with empty suggestions serialize and deserialize correctly.
    /// </summary>
    [Fact]
    public void Serialization_works_with_empty_suggestions()
    {
        var original = CreateSample(suggestions: Array.Empty<IndexSuggestion>());
        var json = original.ToJson();
        var deserialized = SlowQuerySampleJsonExtensions.FromJson(json);

        Assert.NotNull(deserialized);
        Assert.Empty(deserialized.Suggestions);
    }

    /// <summary>
    /// Verifies that samples with null parameters serialize and deserialize correctly.
    /// </summary>
    [Fact]
    public void Serialization_works_with_null_parameters()
    {
        var original = CreateSample(parameters: null);
        var json = original.ToJson();
        var deserialized = SlowQuerySampleJsonExtensions.FromJson(json);

        Assert.NotNull(deserialized);
        Assert.Null(deserialized.Parameters);
    }

    /// <summary>
    /// Verifies that TryFromJson returns false for whitespace-only strings.
    /// </summary>
    [Fact]
    public void TryFromJson_returns_false_for_whitespace_string()
    {
        var result = SlowQuerySampleJsonExtensions.TryFromJson("   \t\n  ", out var value);
        Assert.False(result);
        Assert.Null(value);
    }
}