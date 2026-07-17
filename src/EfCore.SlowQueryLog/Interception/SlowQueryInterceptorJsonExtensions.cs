using System.Text.Json;
using System.Text.Json.Serialization;

namespace EfCore.SlowQueryLog.Interception;

/// <summary>
/// Provides JSON serialization and deserialization extensions for the <see cref="SlowQueryInterceptor"/> class.
/// </summary>
public static class SlowQueryInterceptorJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Serializes the specified <see cref="SlowQueryInterceptor"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="SlowQueryInterceptor"/> instance to serialize.</param>
    /// <param name="indented">A value indicating whether the JSON string should be formatted with indentation.</param>
    /// <returns>A JSON string representing the <see cref="SlowQueryInterceptor"/> instance.</returns>
    public static string ToJson(this SlowQueryInterceptor value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(value, _jsonSerializerOptions);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="SlowQueryInterceptor"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="SlowQueryInterceptor"/> instance representing the deserialized JSON string, or <c>null</c> if deserialization fails.</returns>
    public static SlowQueryInterceptor? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            return JsonSerializer.Deserialize<SlowQueryInterceptor>(json, _jsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="SlowQueryInterceptor"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized <see cref="SlowQueryInterceptor"/> instance, or <c>null</c> if deserialization fails.</param>
    /// <returns>A value indicating whether the deserialization was successful.</returns>
    public static bool TryFromJson(string json, out SlowQueryInterceptor? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            value = JsonSerializer.Deserialize<SlowQueryInterceptor>(json, _jsonSerializerOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
