using ApiGateway.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Net;

namespace ApiGateway.Authorization
{
    public class IpAllowlistAuthorizationHandler : AuthorizationHandler<IpAllowlistRequirement>
    {
        public const string RedisKey = "docs-access:allowed-networks";

        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<IpAllowlistAuthorizationHandler> _logger;
        private readonly DocsAccessSettings _settings;

        public IpAllowlistAuthorizationHandler(IConnectionMultiplexer connectionMultiplexer,
            IHttpContextAccessor httpContextAccessor, ILogger<IpAllowlistAuthorizationHandler> logger,
            IOptions<DocsAccessSettings> settings)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _settings = settings.Value;
        }

        protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, IpAllowlistRequirement requirement)
        {
            if (!_settings.RequireIpAllowlist)
            {
                context.Succeed(requirement);
                return;
            }

            var remoteIp = _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress;

            if (remoteIp is not null && remoteIp.IsIPv4MappedToIPv6)
            {
                remoteIp = remoteIp.MapToIPv4();
            }

            var database = _connectionMultiplexer.GetDatabase();
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
                _logger.LogWarning("Blocked docs access attempt from {RemoteIp}", remoteIp);
            }
        }

        private static IPNetwork? TryParseNetwork(string value)
        {
            return IPNetwork.TryParse(value, out var network) ? network : null;
        }
    }
}