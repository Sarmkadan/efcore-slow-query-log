using System;
using EfCore.SlowQueryLog.Reporting;
using Xunit;

namespace EfCore.SlowQueryLog.Tests;

/// <summary>
/// Tests for the <see cref="SqlSanitizer"/> class.
/// </summary>
public class SqlSanitizerTests
{
    [Theory]
    [InlineData('\\')]
    [InlineData('`')]
    [InlineData('*')]
    [InlineData('_')]
    [InlineData('{')]
    [InlineData('}')]
    [InlineData('[')]
    [InlineData(']')]
    [InlineData('(')]
    [InlineData(')')]
    [InlineData('#')]
    [InlineData('+')]
    [InlineData('-')]
    [InlineData('.')]
    [InlineData('!')]
    [InlineData('|')]
    public void EscapeMarkdown_EscapesEachSpecialCharacter(char specialCharacter)
    {
        Assert.Equal($"\\{specialCharacter}", SqlSanitizer.EscapeMarkdown(specialCharacter.ToString()));
    }

    [Fact]
    public void EscapeMarkdown_PassesPlainTextThrough()
    {
        Assert.Equal("SELECT 1", SqlSanitizer.EscapeMarkdown("SELECT 1"));
    }

    [Fact]
    public void EscapeMarkdown_ThrowsForNull()
    {
        Assert.Throws<ArgumentNullException>(() => SqlSanitizer.EscapeMarkdown(null!));
    }

    [Fact]
    public void EscapeMarkdown_ReturnsEmptyForEmptyInput()
    {
        Assert.Empty(SqlSanitizer.EscapeMarkdown(string.Empty));
    }

    [Fact]
    public void SanitizeSql_WithZeroMaxLength_DoesNotTruncate()
    {
        Assert.Equal("SELECT \\* FROM Users", SqlSanitizer.SanitizeSql("SELECT * FROM Users", 0));
    }

    [Fact]
    public void SanitizeSql_WithMaxLength_TruncatesAndAppendsEllipsis()
    {
        Assert.Equal("abcde...", SqlSanitizer.SanitizeSql("abcdefghij", 8));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("NULL")]
    [InlineData("(none)")]
    public void RedactParameterValue_PassesSpecialValuesThrough(string? value)
    {
        Assert.Equal(value, SqlSanitizer.RedactParameterValue(value!));
    }

    [Fact]
    public void RedactParameterValue_RedactsSensitiveParameterName()
    {
        Assert.Equal("?", SqlSanitizer.RedactParameterValue("hunter2", "@password"));
    }

    [Fact]
    public void RedactParameterValue_RedactsSensitiveValue()
    {
        Assert.Equal("?", SqlSanitizer.RedactParameterValue("contains-a-secret"));
    }

    [Fact]
    public void RedactParameterValue_TruncatesLongValues()
    {
        var value = new string('x', 1001);

        var result = SqlSanitizer.RedactParameterValue(value);

        Assert.Equal(new string('x', 500) + "... [truncated]", result);
    }

    [Fact]
    public void SanitizeParameters_RedactsOnlySensitiveEntries()
    {
        Assert.Equal("@id=5, @password=?", SqlSanitizer.SanitizeParameters("@id=5, @password=hunter2"));
    }

    [Fact]
    public void SanitizeParameters_PassesNonKeyValuePartsThrough()
    {
        Assert.Equal("command text, @id=5", SqlSanitizer.SanitizeParameters("command text, @id=5"));
    }

    [Fact]
    public void SanitizeParameter_FormatsNameAndValue()
    {
        Assert.Equal("@count=42", SqlSanitizer.SanitizeParameter("@count", 42));
    }

    [Fact]
    public void SanitizeParameter_ThrowsForNullName()
    {
        Assert.Throws<ArgumentNullException>(() => SqlSanitizer.SanitizeParameter(null!, 42));
    }

    [Fact]
    public void SanitizeParameter_RendersNullValueAsNullLiteral()
    {
        Assert.Equal("@value=NULL", SqlSanitizer.SanitizeParameter("@value", null));
    }
}
