using ApiGateway.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace ApiGateway.Authorization
{
    public static class DocsAllowlistSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var connectionMultiplexer = scope.ServiceProvider.GetRequiredService<IConnectionMultiplexer>();
            var database = connectionMultiplexer.GetDatabase();

            if (await database.KeyExistsAsync(IpAllowlistAuthorizationHandler.RedisKey))
            {
                return;
            }

            var defaultNetworks = scope.ServiceProvider.GetRequiredService<IOptions<DocsAccessSettings>>().Value.AllowedNetworks;

            if (defaultNetworks.Count > 0)
            {
                var entries = defaultNetworks
                    .Select(network => new HashEntry(network, "Bootstrap default"))
                    .ToArray();

                await database.HashSetAsync(IpAllowlistAuthorizationHandler.RedisKey, entries);
            }
        }
    }
}