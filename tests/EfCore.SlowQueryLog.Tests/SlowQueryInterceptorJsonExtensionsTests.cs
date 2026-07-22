using System;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Unit tests for the <see cref="SlowQueryInterceptorJsonExtensions"/> class.
/// </summary>
public class SlowQueryInterceptorJsonExtensionsTests
{
    private static SlowQueryInterceptor CreateInterceptor()
    {
        // Minimal options required for the interceptor to be instantiated.
        var options = new SlowQueryLogOptions
        {
            Threshold = TimeSpan.FromMilliseconds(1)
        };
        return new SlowQueryInterceptor(options);
    }

    [Fact]
    public void ToJson_Returns_NonEmpty_String()
    {
        var interceptor = CreateInterceptor();

        var json = interceptor.ToJson();

        Assert.False(string.IsNullOrWhiteSpace(json));
        // The JSON should contain the public property name "ranking" (case‑camelized by the serializer)
        Assert.Contains("\"ranking\"", json, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void FromJson_With_Valid_Json_Returns_Null()
    {
        var interceptor = CreateInterceptor();
        var json = interceptor.ToJson();

        var result = SlowQueryInterceptorJsonExtensions.FromJson(json);

        // Because SlowQueryInterceptor has no parameterless constructor or settable properties,
        // deserialization cannot succeed and the method returns null.
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_With_Null_Argument_Throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SlowQueryInterceptorJsonExtensions.FromJson(null!));
    }

    [Fact]
    public void FromJson_With_Empty_String_Throws_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SlowQueryInterceptorJsonExtensions.FromJson(string.Empty));
    }

    [Fact]
    public void TryFromJson_With_Valid_Json_Returns_False_And_Null()
    {
        var interceptor = CreateInterceptor();
        var json = interceptor.ToJson();

        var success = SlowQueryInterceptorJsonExtensions.TryFromJson(json, out var result);

        Assert.False(success);
        Assert.Null(result);
    }

    [Fact]
    public void TryFromJson_With_Null_Argument_Throws_ArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => SlowQueryInterceptorJsonExtensions.TryFromJson(null!, out _));
    }

    [Fact]
    public void TryFromJson_With_Empty_String_Throws_ArgumentException()
    {
        Assert.Throws<ArgumentException>(() => SlowQueryInterceptorJsonExtensions.TryFromJson(string.Empty, out _));
    }
}
