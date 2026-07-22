using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace EfCore.SlowQueryLog;

/// <summary>
/// Extension methods for registering the slow query interceptor in the dependency injection container.
/// </summary>
public static class SlowQueryLogServiceCollectionExtensions
{
    /// <summary>
    /// Adds the slow query interceptor to the service collection with default options.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddSlowQueryLog(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        return services.AddSlowQueryLog(static _ => { });
    }

    /// <summary>
    /// Adds the slow query interceptor to the service collection with custom configuration.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configure">Optional configuration action for the interceptor options.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddSlowQueryLog(
        this IServiceCollection services,
        Action<SlowQueryLogOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new SlowQueryLogOptions();
        configure?.Invoke(options);
        options.Validate();

        return services.AddSingleton<SlowQueryInterceptor>()
            .AddSingleton<IDbCommandInterceptor>(sp => sp.GetRequiredService<SlowQueryInterceptor>());
    }

    /// <summary>
    /// Adds the slow query interceptor to the service collection with a pre-built interceptor instance.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="interceptor">The <see cref="SlowQueryInterceptor"/> instance to register.</param>
    /// <returns>The configured <see cref="IServiceCollection"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="interceptor"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddSlowQueryLog(
        this IServiceCollection services,
        SlowQueryInterceptor interceptor)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(interceptor);

        return services.AddSingleton(interceptor)
            .AddSingleton<IDbCommandInterceptor>(interceptor);
    }
}
