using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfCore.SlowQueryLog;

/// <summary>
/// Registration helpers for wiring the slow query interceptor into a DbContext.
/// </summary>
public static class SlowQueryLogExtensions
{
    /// <summary>
    /// Adds the slow query interceptor to the given options builder.
    /// </summary>
    /// <example>
    /// <code>
    /// optionsBuilder.UseSlowQueryLog(o =>
    /// {
    /// o.Threshold = TimeSpan.FromMilliseconds(200);
    /// o.SuggestIndexes = true;
    /// });
    /// </code>
    /// </example>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <param name="configure">Optional configuration action.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    /// <exception cref="ArgumentNullException"><paramref name="optionsBuilder"/> is <see langword="null"/>.</exception>
    public static DbContextOptionsBuilder UseSlowQueryLog(
        this DbContextOptionsBuilder optionsBuilder,
        Action<SlowQueryLogOptions>? configure = null,
        ILoggerFactory? loggerFactory = null)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);

        var options = new SlowQueryLogOptions();
        configure?.Invoke(options);
        options.Validate();

        var logger = loggerFactory?.CreateLogger<SlowQueryInterceptor>();
        return optionsBuilder.AddInterceptors(new SlowQueryInterceptor(options, logger));
    }

    /// <summary>
    /// Adds a pre-built interceptor instance. Handy when you want to hold a reference to
    /// its <see cref="Reporting.SlowQueryRanking"/> for later reporting.
    /// </summary>
    /// <param name="optionsBuilder">The options builder.</param>
    /// <param name="interceptor">The interceptor instance to add.</param>
    /// <returns>The configured options builder.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="optionsBuilder"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interceptor"/> is <see langword="null"/>.</exception>
    public static DbContextOptionsBuilder UseSlowQueryLog(
        this DbContextOptionsBuilder optionsBuilder,
        SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(interceptor);
        return optionsBuilder.AddInterceptors(interceptor);
    }
}