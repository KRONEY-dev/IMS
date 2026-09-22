using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;
using Shared.Contracts.Messaging;
using StackExchange.Redis;
using System.Text.Json;

namespace InventoryService.Infrastructure.RealTime
{
    public class UserAccessChangeSubscriberHostedService : BackgroundService
    {
        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly IUserConnectionRegistry _userConnectionRegistry;
        private readonly IHubContext<InventoryHub> _hubContext;
        private readonly ILogger<UserAccessChangeSubscriberHostedService> _logger;

        public UserAccessChangeSubscriberHostedService(IConnectionMultiplexer connectionMultiplexer,
            IUserConnectionRegistry userConnectionRegistry, IHubContext<InventoryHub> hubContext,
            ILogger<UserAccessChangeSubscriberHostedService> logger)
        {
            _connectionMultiplexer = connectionMultiplexer;
            _userConnectionRegistry = userConnectionRegistry;
            _hubContext = hubContext;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var subscriber = _connectionMultiplexer.GetSubscriber();

            await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannels.UserWarehouseChanged),
                (_, message) => SafeHandle(() => OnWarehouseChangedAsync(message, stoppingToken)));

            await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannels.UserAccessRevoked),
                (_, message) => SafeHandle(() => OnAccessRevokedAsync(message, stoppingToken)));

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        private async void SafeHandle(Func<Task> handler)
        {
            try
            {
                await handler();
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Failed to process a user access change notification.");
            }
        }

        private async Task OnWarehouseChangedAsync(RedisValue message, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<UserWarehouseAccessChangedEvent>((string)message!)!;
            var connectionIds = await _userConnectionRegistry.GetConnectionIdsAsync(payload.UserId, cancellationToken);
            var groupName = InventoryHub.GroupName(payload.WarehouseId);

            foreach (var connectionId in connectionIds)
            {
                if (payload.Added)
                {
                    await _hubContext.Groups.AddToGroupAsync(connectionId, groupName, cancellationToken);
                }
                else
                {
                    await _hubContext.Groups.RemoveFromGroupAsync(connectionId, groupName, cancellationToken);
                }
            }
        }

        private async Task OnAccessRevokedAsync(RedisValue message, CancellationToken cancellationToken)
        {
            var payload = JsonSerializer.Deserialize<UserAccessRevokedEvent>((string)message!)!;
            var connectionIds = await _userConnectionRegistry.GetConnectionIdsAsync(payload.UserId, cancellationToken);

            await _hubContext.Clients.Clients(connectionIds).SendAsync("ForceDisconnect", cancellationToken);
        }
    }
}
