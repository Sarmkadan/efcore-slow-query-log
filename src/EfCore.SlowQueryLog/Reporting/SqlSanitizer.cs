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
    // Parameter values longer than this are truncated to limit report size.
    private const int MaxParameterValueLength = 1000;

    // Number of characters retained when truncating a long parameter value.
    private const int TruncatedValueLength = 500;

    // Marker appended to parameter values that have been truncated.
    private const string TruncationSuffix = "... [truncated]";

    // Marker appended to SQL text that exceeds its configured maximum length.
    private const string Ellipsis = "...";

    // Replacement used when a parameter value contains sensitive information.
    private const string RedactedPlaceholder = "?";

    // Delimiter used between formatted parameter entries.
    private const string ParameterSeparator = ", ";

    // Common sensitive keywords that should trigger redaction based on parameter names.
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

    // Sensitive keywords that should trigger redaction when found in parameter values.
    private static readonly HashSet<string> _sensitiveValueKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "password",
        "pwd",
        "secret",
        "token",
        "bearer",
        "credential",
        "ssn",
        "creditcard"
    };

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
    /// <param name="maxLength">Maximum length of the SQL text before Markdown escaping (0 for no limit).</param>
    /// <returns>The sanitized SQL text.</returns>
    /// <exception cref="ArgumentNullException">Thrown if sql is null.</exception>
    public static string SanitizeSql(string sql, int maxLength = 0)
    {
        ArgumentNullException.ThrowIfNull(sql);

        if (maxLength > 0 && sql.Length > maxLength)
        {
            sql = maxLength <= Ellipsis.Length
                ? sql.Substring(0, maxLength)
                : sql.Substring(0, maxLength - Ellipsis.Length) + Ellipsis;
        }

        return EscapeMarkdown(sql);
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
                    return RedactedPlaceholder;
                }
            }
        }

        // Check if value contains sensitive patterns
        foreach (var keyword in _sensitiveValueKeywords)
        {
            if (parameterValue.Contains(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return RedactedPlaceholder;
            }
        }

        // For long values, redact to prevent log explosion
        if (parameterValue.Length > MaxParameterValueLength)
        {
            return parameterValue.Substring(0, TruncatedValueLength) + TruncationSuffix;
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
        var parts = formattedParameters.Split(new[] { ParameterSeparator }, StringSplitOptions.None);
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

        return string.Join(ParameterSeparator, sanitizedParts);
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
