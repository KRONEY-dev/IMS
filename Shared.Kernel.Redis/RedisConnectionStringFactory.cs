using StackExchange.Redis;

namespace Shared.Kernel.Redis
{
    public static class RedisConnectionStringFactory
    {
        // AbortOnConnectFail defaults to true, so a single slow first connect throws instead of
        // retrying in the background - fatal for a consumer like the SignalR Redis backplane,
        // which only ever attempts that first connection once.
        public static string BuildResilient(string connectionString)
        {
            var options = ConfigurationOptions.Parse(connectionString);
            options.AbortOnConnectFail = false;

            return options.ToString();
        }
    }
}
