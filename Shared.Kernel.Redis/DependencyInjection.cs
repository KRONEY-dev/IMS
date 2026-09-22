using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Kernel.Caching;
using StackExchange.Redis;

namespace Shared.Kernel.Redis
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddRedisAccessTokenBlacklist(this IServiceCollection services, IConfiguration configuration)
        {
            AddConnectionMultiplexer(services, configuration);

            services.AddSingleton<IAccessTokenBlacklist, RedisAccessTokenBlacklist>();

            return services;
        }

        public static IServiceCollection AddRedisRateLimiter(this IServiceCollection services, IConfiguration configuration)
        {
            AddConnectionMultiplexer(services, configuration);

            services.AddSingleton<IRateLimiter, RedisRateLimiter>();

            return services;
        }

        public static IServiceCollection AddRedisDistributedLock(this IServiceCollection services, IConfiguration configuration)
        {
            AddConnectionMultiplexer(services, configuration);

            services.AddSingleton<IDistributedLock, RedisDistributedLock>();

            return services;
        }

        public static IServiceCollection AddRedisUserAccessRevocation(this IServiceCollection services, IConfiguration configuration)
        {
            AddConnectionMultiplexer(services, configuration);

            services.AddSingleton<IUserAccessRevocation, RedisUserAccessRevocation>();

            return services;
        }

        // TryAdd, not Add: safe to call from multiple Add* methods above — only the first registration
        // wins, so both consumers share one IConnectionMultiplexer regardless of call order or which
        // Add* methods are actually used.
        private static void AddConnectionMultiplexer(IServiceCollection services, IConfiguration configuration)
        {
            services.TryAddSingleton<IConnectionMultiplexer>(
                _ => ConnectionMultiplexer.Connect(RedisConnectionStringFactory.BuildResilient(configuration.GetConnectionString("Redis")!)));
        }
    }
}
