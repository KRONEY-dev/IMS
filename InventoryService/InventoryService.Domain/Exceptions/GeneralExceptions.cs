namespace InventoryService.Domain.Exceptions
{
    public static class GeneralExceptions
    {
        public abstract class GeneralException : Exception
        {
            protected GeneralException(string message) : base(message) { }
        }

        public class InvalidWorkingHoursException()
            : GeneralException("Working hours must contain exactly one entry for each day of the week.");

        public class WarehouseNameAlreadyTakenException(string name)
            : GeneralException($"Warehouse name '{name}' is already taken.")
        {
            public string Name { get; } = name;
        }

        public class NegativeValueException(string fieldName, decimal value)
            : GeneralException($"'{fieldName}' cannot be negative. Value: '{value}'.")
        {
            public string FieldName { get; } = fieldName;
            public decimal Value { get; } = value;
        }

        public class StockTransferNotInTransitException(Guid transferId)
            : GeneralException($"Stock transfer '{transferId}' is not in transit.")
        {
            public Guid TransferId { get; } = transferId;
        }

        public class SupplierOrderNotCreatedException(Guid orderId)
            : GeneralException($"Supplier order '{orderId}' is not in the Created status.")
        {
            public Guid OrderId { get; } = orderId;
        }

        public class SupplierOrderNotSubmittedException(Guid orderId)
            : GeneralException($"Supplier order '{orderId}' is not in the Submitted status.")
        {
            public Guid OrderId { get; } = orderId;
        }

        public class LowStockAlertAlreadyResolvedException(Guid alertId)
            : GeneralException($"Low stock alert '{alertId}' is already resolved.")
        {
            public Guid AlertId { get; } = alertId;
        }

        public class StockThresholdAlreadyExistsException(Guid productId, Guid warehouseId)
            : GeneralException($"Stock threshold for product '{productId}' at warehouse '{warehouseId}' already exists.")
        {
            public Guid ProductId { get; } = productId;
            public Guid WarehouseId { get; } = warehouseId;
        }

        public class InsufficientStockAvailableException(Guid stockItemId, int requestedQuantity)
            : GeneralException($"Stock item '{stockItemId}' does not have sufficient quantity available for '{requestedQuantity}'.")
        {
            public Guid StockItemId { get; } = stockItemId;
            public int RequestedQuantity { get; } = requestedQuantity;
        }

        public class StockItemPriceMismatchException(Guid stockItemId, decimal expectedPrice, decimal actualPrice)
            : GeneralException($"Stock item '{stockItemId}' has price '{expectedPrice}', but received price '{actualPrice}'.")
        {
            public Guid StockItemId { get; } = stockItemId;
            public decimal ExpectedPrice { get; } = expectedPrice;
            public decimal ActualPrice { get; } = actualPrice;
        }

        public class StockItemBatchMismatchException(Guid stockItemId, Guid expectedBatchId, Guid actualBatchId)
            : GeneralException($"Stock item '{stockItemId}' belongs to batch '{expectedBatchId}', but received batch '{actualBatchId}'.")
        {
            public Guid StockItemId { get; } = stockItemId;
            public Guid ExpectedBatchId { get; } = expectedBatchId;
            public Guid ActualBatchId { get; } = actualBatchId;
        }

        public class ShipmentInitiationFailedException(Guid shipmentId)
            : GeneralException($"Shipment '{shipmentId}' could not be initiated: one or more items have insufficient stock available.")
        {
            public Guid ShipmentId { get; } = shipmentId;
        }

        public class SupplierOrderPartialReceiptNotSupportedException(Guid orderId)
            : GeneralException($"Supplier order '{orderId}' must be received in full: the received items must exactly match the order items.")
        {
            public Guid OrderId { get; } = orderId;
        }

        public class StockTransferQuantityExceedsRemainingException(Guid transferId, int requestedQuantity, int remainingQuantity)
            : GeneralException($"Stock transfer '{transferId}' has only '{remainingQuantity}' remaining, but '{requestedQuantity}' was requested.")
        {
            public Guid TransferId { get; } = transferId;
            public int RequestedQuantity { get; } = requestedQuantity;
            public int RemainingQuantity { get; } = remainingQuantity;
        }
    }
}