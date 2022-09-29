namespace CM.Infrastructure.Consul.Services
{
    internal interface IServiceIdProvider
    {
        string GetUniqueServiceId();
    }
}
