using System;
using System.Collections.Generic;
using System.Text;

namespace EfCore.SlowQueryLog.Reporting;

/// <summary>
/// Provides sanitization and validation for SQL text and parameter values to prevent
/// injection attacks and information disclosure in reports.
/// </summary>
public static class SqlSanitizer
{
    // Common sensitive keywords that should be redacted from parameter values
    private static readonly HashSet<string> _sensitiveKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "pwd",
        "secret",
        "token",
        "api",
        "key",
        "credential",
        "auth",
        "bearer",
        "cookie",
        "session",
        "creditcard",
        "ssn",
        "socialsecurity",
        "email",
        "phone",
        "address",
        "user",
        "username"
    };

    // Markdown special characters that need escaping
    private static readonly char[] _markdownSpecialChars = new[] { '\\', '`', '*', '_', '{', '}', '[', ']', '(', ')', '#', '+', '-', '.', '!', '|' };

    /// <summary>
    /// Escapes Markdown special characters in SQL text to prevent Markdown injection.
    /// </summary>
    /// <param name="sql">The SQL text to escape.</param>
    /// <returns>The escaped SQL text safe for Markdown output.</returns>
    /// <exception cref="ArgumentNullException">Thrown if sql is null.</exception>
    public static string EscapeMarkdown(string sql)
    {
        ArgumentNullException.ThrowIfNull(sql);

        if (sql.Length == 0)
        {
            return sql;
        }

        var sb = new StringBuilder(sql.Length);
        foreach (var c in sql)
        {
            if (IsMarkdownSpecialChar(c))
            {
                sb.Append('\\');
                sb.Append(c);
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Checks if a character is a Markdown special character that needs escaping.
    /// </summary>
    private static bool IsMarkdownSpecialChar(char c)
    {
        return c switch
        {
            '\\' or '`' or '*' or '_' or '{' or '}' or '[' or ']' or '(' or ')' or '#' or '+' or '-' or '.' or '!' or '|' => true,
            _ => false
        };
    }

    /// <summary>
    /// Sanitizes SQL text for safe output in reports.
    /// </summary>
    /// <param name="sql">The SQL text to sanitize.</param>
    /// <param name="maxLength">Maximum length of the sanitized output (0 for no limit).</param>
    /// <returns>The sanitized SQL text.</returns>
    /// <exception cref="ArgumentNullException">Thrown if sql is null.</exception>
    public static string SanitizeSql(string sql, int maxLength = 0)
    {
        ArgumentNullException.ThrowIfNull(sql);

        var sanitized = EscapeMarkdown(sql);

        if (maxLength > 0 && sanitized.Length > maxLength)
        {
            sanitized = sanitized.Substring(0, maxLength - 3) + "...";
        }

        return sanitized;
    }

    /// <summary>
    /// Redacts sensitive information from a parameter value string.
    /// </summary>
    /// <param name="parameterValue">The parameter value string to redact.</param>
    /// <param name="parameterName">The parameter name (used to detect sensitive parameters).</param>
    /// <returns>The redacted parameter value.</returns>
    public static string RedactParameterValue(string parameterValue, string? parameterName = null)
    {
        if (string.IsNullOrEmpty(parameterValue) || parameterValue == "NULL" || parameterValue == "(none)")
        {
            return parameterValue;
        }

        // Check if parameter name contains sensitive keywords
        if (parameterName != null)
        {
            foreach (var keyword in _sensitiveKeywords)
            {
                if (parameterName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return "?";
                }
            }
        }

        // Check if value contains sensitive patterns
        var lowerValue = parameterValue.ToLowerInvariant();
        foreach (var keyword in _sensitiveKeywords)
        {
            if (lowerValue.Contains(keyword))
            {
                return "?";
            }
        }

        // For long values, redact to prevent log explosion
        if (parameterValue.Length > 1000)
        {
            return parameterValue.Substring(0, 500) + "... [truncated]";
        }

        return parameterValue;
    }

    /// <summary>
    /// Sanitizes a formatted parameter string for safe output.
    /// </summary>
    /// <param name="formattedParameters">The formatted parameter string (e.g., "@param1=value1, @param2=value2").</param>
    /// <returns>The sanitized parameter string.</returns>
    public static string SanitizeParameters(string formattedParameters)
    {
        ArgumentNullException.ThrowIfNull(formattedParameters);

        if (formattedParameters.Length == 0)
        {
            return formattedParameters;
        }

        // Split by comma to handle individual parameters
        var parts = formattedParameters.Split(new[] { ", " }, StringSplitOptions.None);
        var sanitizedParts = new List<string>(parts.Length);

        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            // Extract parameter name and value
            var equalsIndex = part.IndexOf('=');
            if (equalsIndex > 0)
            {
                var paramName = part.Substring(0, equalsIndex);
                var paramValue = equalsIndex < part.Length - 1 ? part.Substring(equalsIndex + 1) : "";

                // Redact the value
                var redactedValue = RedactParameterValue(paramValue, paramName);
                sanitizedParts.Add($"{paramName}={redactedValue}");
            }
            else
            {
                sanitizedParts.Add(part);
            }
        }

        return string.Join(", ", sanitizedParts);
    }

    /// <summary>
    /// Sanitizes a parameter entry for safe logging/reporting.
    /// </summary>
    /// <param name="parameterName">The parameter name.</param>
    /// <param name="parameterValue">The parameter value.</param>
    /// <returns>A sanitized representation of the parameter.</returns>
    public static string SanitizeParameter(string parameterName, object? parameterValue)
    {
        ArgumentNullException.ThrowIfNull(parameterName);

        var valueStr = parameterValue?.ToString() ?? "NULL";
        return $"{parameterName}={RedactParameterValue(valueStr, parameterName)}";
    }
}
