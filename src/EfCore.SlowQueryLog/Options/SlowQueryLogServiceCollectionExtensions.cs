using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using EfCore.SlowQueryLog.Options;

namespace EfCore.SlowQueryLog.Options;

/// <summary>
/// Extension methods for registering <see cref="SlowQueryLogOptions"/> validation with the DI container.
/// </summary>
public static class SlowQueryLogServiceCollectionExtensions
{
    /// <summary>
    /// Registers the <see cref="SlowQueryLogOptionsValidator"/> so that the options system validates
    /// <see cref="SlowQueryLogOptions"/> on startup or when the options are bound.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the validator to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddSlowQueryLogOptionsValidation(this IServiceCollection services)
    {
        // Register the validator as a singleton so the options system can discover it.
        services.AddSingleton<IValidateOptions<SlowQueryLogOptions>, SlowQueryLogOptionsValidator>();
        return services;
    }
}
