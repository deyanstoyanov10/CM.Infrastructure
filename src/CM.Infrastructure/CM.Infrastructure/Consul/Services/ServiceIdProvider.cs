namespace CM.Infrastructure.Consul.Services
{
    using System;

    internal class ServiceIdProvider : IServiceIdProvider
    {
        private readonly string _id;

        public ServiceIdProvider() => _id = Guid.NewGuid().ToString().Substring(0, 10);

        public string GetUniqueServiceId()
            => _id;
    }
}
