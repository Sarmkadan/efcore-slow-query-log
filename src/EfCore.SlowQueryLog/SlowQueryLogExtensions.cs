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
    /// optionsBuilder.UseSlowQueryLog(o =&gt;
    /// {
    ///     o.Threshold = TimeSpan.FromMilliseconds(200);
    ///     o.SuggestIndexes = true;
    /// });
    /// </code>
    /// </example>
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
    public static DbContextOptionsBuilder UseSlowQueryLog(
        this DbContextOptionsBuilder optionsBuilder,
        SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(optionsBuilder);
        ArgumentNullException.ThrowIfNull(interceptor);
        return optionsBuilder.AddInterceptors(interceptor);
    }
}
