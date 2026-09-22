using AccountsService.Application.Options;
using AccountsService.Application.Services.Interfaces;
using Microsoft.Extensions.Options;
using Shared.Contracts.Events;
using Shared.Contracts.Messaging;
using Shared.Kernel.Caching;
using StackExchange.Redis;
using System.Text.Json;

namespace AccountsService.Infrastructure.Services
{
    public class UserAccessChangeNotifierService : IUserAccessChangeNotifier
    {
        private readonly IUserAccessRevocation _userAccessRevocation;
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly JwtSettings _jwtSettings;

        public UserAccessChangeNotifierService(IUserAccessRevocation userAccessRevocation,
            IConnectionMultiplexer connectionMultiplexer, IOptions<JwtSettings> jwtSettings)
        {
            _userAccessRevocation = userAccessRevocation;
            _connectionMultiplexer = connectionMultiplexer;
            _jwtSettings = jwtSettings.Value;
        }

        public async Task NotifyWarehouseChangedAsync(Guid userId, Guid warehouseId, bool added, CancellationToken cancellationToken)
        {
            await _userAccessRevocation.RevokeAsync(userId, _jwtSettings.AccessTokenLifetime, cancellationToken);

            var payload = JsonSerializer.Serialize(new UserWarehouseAccessChangedEvent(userId, warehouseId, added));
            await _connectionMultiplexer.GetSubscriber().PublishAsync(RedisChannel.Literal(RedisChannels.UserWarehouseChanged), payload);
        }

        public async Task NotifyAccessRevokedAsync(Guid userId, CancellationToken cancellationToken)
        {
            await _userAccessRevocation.RevokeAsync(userId, _jwtSettings.AccessTokenLifetime, cancellationToken);

            var payload = JsonSerializer.Serialize(new UserAccessRevokedEvent(userId));
            await _connectionMultiplexer.GetSubscriber().PublishAsync(RedisChannel.Literal(RedisChannels.UserAccessRevoked), payload);
        }
    }
}
