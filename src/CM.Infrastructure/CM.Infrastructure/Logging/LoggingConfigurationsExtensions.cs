namespace CM.Infrastructure.Logging
{
    using Configurations;
    
    using Serilog;
    using Serilog.Core;
    using Serilog.Events;

    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Configuration;

    using System;

    public static class LoggingConfigurationsExtensions
    {
        public static IHostBuilder ConfigureLogging(this IHostBuilder hostBuilder, IConfiguration configuration)
        {
            var serilogConfig = configuration.GetSerilogConfiguration();

            var logLevelSwitch = new LoggingLevelSwitch();

            var configuredMinimumLevel = LogEventLevel.Information;

            try
            {
                configuredMinimumLevel = Enum.TryParse(serilogConfig.MinimumLevel, out LogEventLevel logEventLevel) ? logEventLevel : LogEventLevel.Information;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            logLevelSwitch.MinimumLevel = configuredMinimumLevel;

            Log.Logger = new LoggerConfiguration()
                .Enrich.FromLogContext()
                .MinimumLevel.ControlledBy(logLevelSwitch)
                .WriteTo.File("log.txt", rollingInterval: RollingInterval.Day, rollOnFileSizeLimit: true)
                .CreateLogger();

            hostBuilder
                .UseSerilog();

            return hostBuilder;
        }

        private static Serilog GetSerilogConfiguration(this IConfiguration configuration)
            => configuration
                    .GetSection(nameof(Serilog))
                    .Get<Serilog>();
    }
}
