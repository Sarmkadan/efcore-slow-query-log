using System;

namespace EfCore.SlowQueryLog.Analysis;

/// <summary>
/// The exception that is thrown when regex parsing exceeds the configured timeout,
/// indicating potential ReDoS vulnerability or malformed input.
/// </summary>
public sealed class RegexParsingException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegexParsingException"/> class.
    /// </summary>
    public RegexParsingException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexParsingException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public RegexParsingException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegexParsingException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public RegexParsingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}