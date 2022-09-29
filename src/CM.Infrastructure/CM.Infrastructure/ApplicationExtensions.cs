namespace CM.Infrastructure
{
    using Infrastructure.Consul;
    using Infrastructure.Logging;
    using Infrastructure.HealthCheck;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;

    public static class ApplicationExtensions
    {
        public static WebApplicationBuilder RegisterCMI(this WebApplicationBuilder builder)
        {
            builder.Services
                            .RegisterConsul(builder.Configuration)
                            .AddHealthChecks();

            builder.Host
                        .ConfigureLogging(builder.Configuration)
                        .RegisterConsulConfigurationProvider();

            return builder;
        }

        public static WebApplication UseCMI(this WebApplication app)
        {
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapServiceHealthCheck();
            });

            return app;
        }
    }
}
