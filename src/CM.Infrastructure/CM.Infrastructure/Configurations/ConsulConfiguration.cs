namespace CM.Infrastructure.Configurations
{
    public class ConsulConfiguration
    {
        public bool Enabled { get; set; }

        public string Url { get; set; }

        public string ServiceName { get; set; }

        public string Address { get; set; }

        public int Port { get; set; }

        public int RetryTimes { get; set; }

        public bool ReloadOnChange { get; set; }
    }
}
