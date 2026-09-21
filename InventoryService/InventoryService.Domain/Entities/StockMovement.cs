using InventoryService.Domain.Entities.Base;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Domain.Entities
{
    public enum StockMovementType
    {
        In,
        Out,
        Adjustment
    }

    public class StockMovement : BaseEntity
    {
        public required Guid StockItemId { get; init; }

        public required Guid ProductId { get; init; }
        public required Guid WarehouseId { get; init; }
        public required Guid BatchId { get; init; }
        public required decimal Price { get; init; }

        public required StockMovementType Type { get; init; }
        public required int Quantity { get; init; }

        public Guid? Reference { get; init; }

        public required Guid InitiatedByUserId { get; init; }
        public required Guid PerformedByUserId { get; init; }

        public required DateTime CreatedAt { get; init; }

        private StockMovement() { }

        public static StockMovement Create(Guid stockItemId, Guid productId, Guid warehouseId, Guid batchId,
            decimal price, StockMovementType type, int quantity, Guid? reference,
            Guid initiatedByUserId, Guid performedByUserId)
        {
            EnsureNonNegative(nameof(quantity), quantity);

            return new StockMovement
            {
                Id = Guid.NewGuid(),
                StockItemId = stockItemId,
                ProductId = productId,
                WarehouseId = warehouseId,
                BatchId = batchId,
                Price = price,
                Type = type,
                Quantity = quantity,
                Reference = reference,
                InitiatedByUserId = initiatedByUserId,
                PerformedByUserId = performedByUserId,
                CreatedAt = DateTime.UtcNow
            };
        }

        private static void EnsureNonNegative(string fieldName, decimal value)
        {
            if (value < 0)
            {
                throw new NegativeValueException(fieldName, value);
            }
        }
    }
}