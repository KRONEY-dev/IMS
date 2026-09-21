using Microsoft.AspNetCore.Authorization;
using StackExchange.Redis;
using System.Net;

namespace ApiGateway.Authorization
{
    public class IpAllowlistAuthorizationHandler(IConnectionMultiplexer connectionMultiplexer,
        IHttpContextAccessor httpContextAccessor, ILogger<IpAllowlistAuthorizationHandler> logger)
        : AuthorizationHandler<IpAllowlistRequirement>
    {
        public const string RedisKey = "docs-access:allowed-networks";

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IpAllowlistRequirement requirement)
        {
            var remoteIp = httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;

            if (remoteIp is not null && remoteIp.IsIPv4MappedToIPv6)
            {
                remoteIp = remoteIp.MapToIPv4();
            }

            var database = connectionMultiplexer.GetDatabase();
            var allowedNetworks = await database.HashKeysAsync(RedisKey);

            var isAllowed = remoteIp is not null && allowedNetworks
                .Select(value => TryParseNetwork(value.ToString()))
                .Where(network => network is not null)
                .Any(network => network!.Value.Contains(remoteIp));

            if (isAllowed)
            {
                context.Succeed(requirement);
            }
            else
            {
                logger.LogWarning("Blocked docs access attempt from {RemoteIp}", remoteIp);
            }
        }

        private static IPNetwork? TryParseNetwork(string value)
        {
            return IPNetwork.TryParse(value, out var network) ? network : null;
        }
    }
}