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
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
    public static string ToJson(this SlowQueryInterceptor value, bool indented = false)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));

        // Clone the base options so we can set WriteIndented without affecting the static instance.
        var options = new JsonSerializerOptions(_jsonSerializerOptions)
        {
            WriteIndented = indented
        };

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="SlowQueryInterceptor"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>A <see cref="SlowQueryInterceptor"/> instance representing the deserialized JSON string, or <c>null</c> if deserialization fails.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="json"/> is empty or whitespace.</exception>
    public static SlowQueryInterceptor? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrEmpty(json, nameof(json));

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
    /// <returns>A value indicating whether the deserialization was successful (i.e., a non‑null instance was created).</returns>
    /// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentException"><paramref name="json"/> is empty or whitespace.</exception>
    public static bool TryFromJson(string json, out SlowQueryInterceptor? value)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrEmpty(json, nameof(json));

        try
        {
            value = JsonSerializer.Deserialize<SlowQueryInterceptor>(json, _jsonSerializerOptions);
            // Consider deserialization successful only if we actually obtained a non‑null instance.
            return value != null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
