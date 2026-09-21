using InventoryService.Domain.Entities.Base;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Domain.Entities
{
    public class StockThreshold : BaseEntity
    {
        public required Guid ProductId { get; init; }
        public required Guid WarehouseId { get; init; }

        public int ReorderLevel { get; private set; }
        public int ReorderQuantity { get; private set; }

        private StockThreshold() { }

        public static StockThreshold Create(Guid productId, Guid warehouseId, int reorderLevel, int reorderQuantity)
        {
            EnsureNonNegative(nameof(reorderLevel), reorderLevel);
            EnsureNonNegativeOrZero(nameof(reorderQuantity), reorderQuantity);

            return new StockThreshold
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                WarehouseId = warehouseId,
                ReorderLevel = reorderLevel,
                ReorderQuantity = reorderQuantity
            };
        }

        public void UpdateThresholds(int reorderLevel, int reorderQuantity)
        {
            EnsureNonNegative(nameof(reorderLevel), reorderLevel);
            EnsureNonNegativeOrZero(nameof(reorderQuantity), reorderQuantity);

            ReorderLevel = reorderLevel;
            ReorderQuantity = reorderQuantity;
        }

        private static void EnsureNonNegative(string fieldName, decimal value)
        {
            if (value < 0)
            {
                throw new NegativeValueException(fieldName, value);
            }
        }

        private static void EnsureNonNegativeOrZero(string fieldName, decimal value)
        {
            if (value <= 0)
            {
                throw new NegativeValueException(fieldName, value);
            }
        }
    }
}