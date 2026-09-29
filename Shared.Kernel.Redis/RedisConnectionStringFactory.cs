using StackExchange.Redis;

namespace Shared.Kernel.Redis
{
    public static class RedisConnectionStringFactory
    {
        public static string BuildResilient(string connectionString)
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;

            return options.ToString();
        }
    }
}