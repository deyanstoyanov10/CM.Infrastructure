using Consul;
using Winton.Extensions.Configuration.Consul;

namespace CM.Infrastructure.Consul
{
    using Services;
    using Configurations;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;

    using System;

    internal static class ConsulConfigurationExtensions
    {
        private static string Default_Settings_Key = "AppSettings";

        internal static IServiceCollection RegisterConsul(this IServiceCollection services, IConfiguration configuration)
            => services
                    .Configure<ConsulConfiguration>(configuration.GetSection(nameof(ConsulConfiguration)))
                    .Configure<FabioConfiguration>(configuration.GetSection(nameof(FabioConfiguration)))
                    .AddSingleton<IConsulClient>(consul => new ConsulClient(consulConfig =>
                    {
                        var consulConfiguration = configuration.GetConsulConfiguration();

                        consulConfig.Address = new Uri(consulConfiguration.Url);
                    }))
                    .AddSingleton<IServiceIdProvider, ServiceIdProvider>()
                    .AddHostedService<ConsulRegistrationService>();

        internal static IHostBuilder RegisterConsulConfigurationProvider(this IHostBuilder hostBuilder)
             => hostBuilder
                .ConfigureAppConfiguration((context, config) =>
                {
                    IConfigurationRoot configuration = config.Build();
                    ConsulConfiguration consulConfiguration = configuration.GetConsulConfiguration();

                    string environmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";

                    config.AddConsul($"{consulConfiguration.ServiceName}/{environmentName}/{Default_Settings_Key}", options =>
                    {
                        options.ConsulConfigurationOptions = cco => cco.Address = new Uri(consulConfiguration.Url);
                        options.Optional = false;
                        options.ReloadOnChange = consulConfiguration.ReloadOnChange;
                        options.OnLoadException = exceptionContext => { exceptionContext.Ignore = true; };
                    });
                });

        internal static ConsulConfiguration GetConsulConfiguration(this IConfiguration configuration)
            => configuration
                        .GetSection(nameof(ConsulConfiguration))
                        .Get<ConsulConfiguration>();
    }
}
