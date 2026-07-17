using System.Text.Json;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="SlowQueryInterceptorTests"/> test class instances.
/// </summary>
public static class SlowQueryInterceptorTestsJsonExtensions
{
 private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
 {
 PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
 WriteIndented = false,
 };

 /// <summary>
 /// Serializes a <see cref="SlowQueryInterceptorTests"/> instance to a JSON string.
 /// </summary>
 /// <param name="value">The instance to serialize.</param>
 /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
 /// <returns>A JSON string representation of the test class.</returns>
 /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
 public static string ToJson(this SlowQueryInterceptorTests value, bool indented = false)
 {
 ArgumentNullException.ThrowIfNull(value);

 var options = indented
 ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
 : _jsonSerializerOptions;

 return JsonSerializer.Serialize(value, options);
 }

 /// <summary>
 /// Deserializes a JSON string to a <see cref="SlowQueryInterceptorTests"/> instance.
 /// </summary>
 /// <param name="json">The JSON string to deserialize.</param>
 /// <returns>The deserialized test class instance.</returns>
 /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
 /// <exception cref="JsonException">Thrown when <paramref name="json"/> is invalid or cannot be deserialized.</exception>
 public static SlowQueryInterceptorTests? FromJson(string json)
 {
 ArgumentNullException.ThrowIfNull(json);

 return JsonSerializer.Deserialize<SlowQueryInterceptorTests>(json, _jsonSerializerOptions);
 }

 /// <summary>
 /// Attempts to deserialize a JSON string to a <see cref="SlowQueryInterceptorTests"/> instance.
 /// </summary>
 /// <param name="json">The JSON string to deserialize.</param>
 /// <param name="value">Receives the deserialized test class instance if successful.</param>
 /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
 /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/> or empty.</exception>
 public static bool TryFromJson(string json, out SlowQueryInterceptorTests? value)
 {
 ArgumentException.ThrowIfNullOrEmpty(json);

 try
 {
 value = JsonSerializer.Deserialize<SlowQueryInterceptorTests>(json, _jsonSerializerOptions);
 return true;
 }
 catch (JsonException)
 {
 value = null;
 return false;
 }
 }
}