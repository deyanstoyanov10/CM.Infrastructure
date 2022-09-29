using Consul;

namespace CM.Infrastructure.Consul.Services
{
    using Configurations;

    using Polly;
    using Polly.Wrap;
    using Polly.Retry;
    using Polly.CircuitBreaker;

    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Extensions.Hosting;

    using System;
    using System.Net;
    using System.Linq;
    using System.Threading;
    using System.Net.Sockets;
    using System.Threading.Tasks;

    internal class ConsulRegistrationService : IHostedService
    {
        private readonly string Default_HealthCheck_Endpoint = "/healthcheck";
        private readonly string Default_Address = "localhost";

        private readonly IConsulClient _consulClient;
        private readonly IServiceIdProvider _serviceIdProvider;
        private readonly ILogger<ConsulRegistrationService> _logger;
        private readonly ConsulConfiguration _consulConfig;
        private readonly FabioConfiguration _fabioConfig;

        public ConsulRegistrationService(
            IConsulClient consulClient,
            IServiceIdProvider serviceIdProvider,
            IOptions<ConsulConfiguration> consulConfig,
            IOptions<FabioConfiguration> fabioConfig,
            ILogger<ConsulRegistrationService> logger)
        {
            _consulClient = consulClient ?? throw new ArgumentException("Consul client not registered.");
            _serviceIdProvider = serviceIdProvider;
            _logger = logger;
            _consulConfig = consulConfig.Value;
            _fabioConfig = fabioConfig.Value;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_consulConfig.Enabled)
            {
                _logger.LogWarning("Skipping consul registration as it's not enabled.");
                return;
            }

            var scheme = "http://";

            var ipAddress = _consulConfig.Address == Default_Address ? GetIpAddress() : _consulConfig.Address;

            Console.WriteLine(ipAddress);

            var check = new AgentServiceCheck()
            {
                Interval = TimeSpan.FromSeconds(15),
                DeregisterCriticalServiceAfter = TimeSpan.FromSeconds(60),
                HTTP = $"{scheme}{ipAddress}:{_consulConfig.Port}{Default_HealthCheck_Endpoint}"
            };

            var registration = new AgentServiceRegistration()
            {
                ID = _serviceIdProvider.GetUniqueServiceId(),
                Name = _consulConfig.ServiceName,
                Address = ipAddress,
                Port = _consulConfig.Port,
                Check = check
            };

            if (_fabioConfig.Enabled)
            {
                registration.Tags = GetFabioTags();
            }

            var result = await _resilientPolicy.ExecuteAsync(async () => await _consulClient.Agent.ServiceRegister(registration, cancellationToken));

            if (result.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Failed to register application in consul.");
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            string serviceId = _serviceIdProvider.GetUniqueServiceId();

            _logger.LogInformation("Deregistering application from consul.");

            var result = await _consulClient.Agent.ServiceDeregister(serviceId);

            if (result.StatusCode != HttpStatusCode.OK)
            {
                _logger.LogError("Failed to deregister application from consul.");
            }
        }

        private string GetIpAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            var ipAddress = host.AddressList.FirstOrDefault(address => address.AddressFamily == AddressFamily.InterNetwork);

            return ipAddress == null ? Default_Address : ipAddress.ToString();
        }

        private string[] GetFabioTags()
        {
            return new[] { $"urlprefix-/{_consulConfig.ServiceName} strip=/{_consulConfig.ServiceName}" };
        }

        private static readonly AsyncRetryPolicy<WriteResult> TransientErrorRetryPolicy =
            Polly.Policy.HandleResult<WriteResult>(result => ((int)result.StatusCode) == 429 || ((int)result.StatusCode) >= 500)
                .WaitAndRetryAsync(2, retryAttempt =>
                {
                    var random = new Random();

                    Console.WriteLine("Retry registering service to consul");

                    return TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)) + TimeSpan.FromMilliseconds(random.Next(0, 1000));
                });

        private static readonly AsyncCircuitBreakerPolicy<WriteResult> CircuitBreakerPolicy =
            Polly.Policy.HandleResult<WriteResult>(result =>
                result.StatusCode == HttpStatusCode.InternalServerError ||
                result.StatusCode == HttpStatusCode.BadGateway ||
                result.StatusCode == HttpStatusCode.ServiceUnavailable)
                .CircuitBreakerAsync(2, TimeSpan.FromMinutes(1));

        private readonly AsyncPolicyWrap<WriteResult> _resilientPolicy =
            CircuitBreakerPolicy.WrapAsync(TransientErrorRetryPolicy);
    }
}
