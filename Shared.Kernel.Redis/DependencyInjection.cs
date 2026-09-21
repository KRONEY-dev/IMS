using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.Caching;
using StackExchange.Redis;

namespace Shared.Kernel.Redis
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRedisAccessTokenBlacklist(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(configuration.GetConnectionString("Redis")!));

            services.AddSingleton<IAccessTokenBlacklist, RedisAccessTokenBlacklist>();

            return services;
        }
    }
}