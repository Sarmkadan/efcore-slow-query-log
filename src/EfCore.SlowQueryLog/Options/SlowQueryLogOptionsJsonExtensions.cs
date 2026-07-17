using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace EfCore.SlowQueryLog.Options;

/// <summary>
/// Provides System.Text.Json serialization and deserialization helpers for <see cref="SlowQueryLogOptions"/>.
/// </summary>
public static class SlowQueryLogOptionsJsonExtensions
{
    private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes a <see cref="SlowQueryLogOptions"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this SlowQueryLogOptions value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_options) { WriteIndented = true }
            : _options;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="SlowQueryLogOptions"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized instance, or null if the JSON is null or empty.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static SlowQueryLogOptions? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return string.IsNullOrEmpty(json)
            ? null
            : JsonSerializer.Deserialize<SlowQueryLogOptions>(json, _options);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="SlowQueryLogOptions"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out SlowQueryLogOptions? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        value = null;

        if (string.IsNullOrEmpty(json))
        {
            return false;
        }

        try
        {
            value = JsonSerializer.Deserialize<SlowQueryLogOptions>(json, _options);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}