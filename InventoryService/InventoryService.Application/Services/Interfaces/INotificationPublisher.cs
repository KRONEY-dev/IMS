using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Services.Interfaces
{
    public interface INotificationPublisher
    {
        Task NotifyStockLevelChangedAsync(
            NotificationServiceDTOs.StockLevelChangedNotification notification, CancellationToken cancellationToken);

        Task NotifyLowStockAlertAsync(
            NotificationServiceDTOs.LowStockAlertNotification notification, CancellationToken cancellationToken);

        Task NotifySupplierOrderReceivedAsync(
            NotificationServiceDTOs.SupplierOrderReceivedNotification notification, CancellationToken cancellationToken);
    }
}
