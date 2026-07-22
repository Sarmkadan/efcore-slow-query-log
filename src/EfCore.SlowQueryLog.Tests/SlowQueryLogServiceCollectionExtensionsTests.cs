using System;
using EfCore.SlowQueryLog;
using EfCore.SlowQueryLog.Interception;
using EfCore.SlowQueryLog.Options;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EfCore.SlowQueryLog.Tests
{
    public class SlowQueryLogServiceCollectionExtensionsTests
    {
        [Fact]
        public void AddSlowQueryLog_NoArgs_RegistersInterceptor()
        {
            // Arrange
            var services = new ServiceCollection();

            // Act
            services.AddSlowQueryLog();

            // Build provider and resolve services
            var provider = services.BuildServiceProvider();

            var interceptor = provider.GetRequiredService<SlowQueryInterceptor>();
            var dbInterceptor = provider.GetRequiredService<IDbCommandInterceptor>();

            // Assert
            Assert.NotNull(interceptor);
            Assert.NotNull(dbInterceptor);
            // The IDbCommandInterceptor registration should resolve to the same instance
            Assert.Same(interceptor, dbInterceptor);
        }

        [Fact]
        public void AddSlowQueryLog_WithConfigure_RegistersInterceptorAndInvokesConfigure()
        {
            // Arrange
            var services = new ServiceCollection();
            var configureCalled = false;
            TimeSpan configuredThreshold = TimeSpan.Zero;

            // Act
            services.AddSlowQueryLog(opts =>
            {
                configureCalled = true;
                configuredThreshold = TimeSpan.FromMilliseconds(123);
                opts.Threshold = configuredThreshold;
            });

            var provider = services.BuildServiceProvider();

            var interceptor = provider.GetRequiredService<SlowQueryInterceptor>();
            var dbInterceptor = provider.GetRequiredService<IDbCommandInterceptor>();

            // Assert
            Assert.True(configureCalled, "Configure action should have been invoked.");
            Assert.NotNull(interceptor);
            Assert.Same(interceptor, dbInterceptor);
            // The configured threshold is stored inside the interceptor's options; we can verify via reflection
            var optionsField = typeof(SlowQueryInterceptor).GetField("_options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            var options = (SlowQueryLogOptions)optionsField!.GetValue(interceptor)!;
            Assert.Equal(configuredThreshold, options.Threshold);
        }

        [Fact]
        public void AddSlowQueryLog_WithNullServices_ThrowsArgumentNullException()
        {
            // Null services for the parameterless overload
            Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddSlowQueryLog());

            // Null services for the configure overload
            Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddSlowQueryLog(_ => { }));

            // Null services for the interceptor overload
            var interceptor = new SlowQueryInterceptor(new SlowQueryLogOptions());
            Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddSlowQueryLog(interceptor));
        }

        [Fact]
        public void AddSlowQueryLog_WithInterceptor_RegistersProvidedInstance()
        {
            // Arrange
            var services = new ServiceCollection();
            var options = new SlowQueryLogOptions { Threshold = TimeSpan.FromMilliseconds(1) };
            var providedInterceptor = new SlowQueryInterceptor(options);

            // Act
            services.AddSlowQueryLog(providedInterceptor);
            var provider = services.BuildServiceProvider();

            var resolvedInterceptor = provider.GetRequiredService<SlowQueryInterceptor>();
            var resolvedDbInterceptor = provider.GetRequiredService<IDbCommandInterceptor>();

            // Assert
            Assert.Same(providedInterceptor, resolvedInterceptor);
            Assert.Same(providedInterceptor, resolvedDbInterceptor);
        }

        [Fact]
        public void AddSlowQueryLog_WithNullInterceptor_ThrowsArgumentNullException()
        {
            var services = new ServiceCollection();
            Assert.Throws<ArgumentNullException>(() => services.AddSlowQueryLog((SlowQueryInterceptor)null!));
        }
    }
}
