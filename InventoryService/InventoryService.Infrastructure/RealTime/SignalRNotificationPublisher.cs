using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace InventoryService.Infrastructure.RealTime
{
    public class SignalRNotificationPublisher : INotificationPublisher
    {
        private const string NotificationSuffix = "Notification";

        private readonly IHubContext<InventoryHub> _hubContext;

        public SignalRNotificationPublisher(IHubContext<InventoryHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public Task NotifyStockLevelChangedAsync(
            NotificationServiceDTOs.StockLevelChangedNotification notification, CancellationToken cancellationToken)
        {
            return NotifyAsync(InventoryHub.GroupName(notification.WarehouseId), notification, cancellationToken);
        }

        public Task NotifyLowStockAlertAsync(
            NotificationServiceDTOs.LowStockAlertNotification notification, CancellationToken cancellationToken)
        {
            return NotifyAsync(InventoryHub.GroupName(notification.WarehouseId), notification, cancellationToken);
        }

        public Task NotifySupplierOrderReceivedAsync(
            NotificationServiceDTOs.SupplierOrderReceivedNotification notification, CancellationToken cancellationToken)
        {
            return NotifyAsync(InventoryHub.GroupName(notification.WarehouseId), notification, cancellationToken);
        }

        private Task NotifyAsync(string groupName, object? payload, CancellationToken cancellationToken)
        {
            var eventName = payload?.GetType().Name ?? string.Empty;

            if (eventName.EndsWith(NotificationSuffix, StringComparison.Ordinal))
            {
                eventName = eventName[..^NotificationSuffix.Length];
            }

            return _hubContext.Clients.Group(groupName).SendAsync(eventName, payload, cancellationToken);
        }
    }
}
