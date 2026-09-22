namespace InventoryService.Application.Services.DTOs
{
    public static class NotificationServiceDTOs
    {
        public record StockLevelChangedNotification(Guid ProductId, Guid WarehouseId);

        public record LowStockAlertNotification(Guid ProductId, Guid WarehouseId);

        public record SupplierOrderReceivedNotification(Guid SupplierOrderId, Guid WarehouseId);
    }
}
