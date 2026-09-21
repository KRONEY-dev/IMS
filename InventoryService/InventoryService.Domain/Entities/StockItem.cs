using InventoryService.Domain.Entities.Base;
using System.Linq.Expressions;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Domain.Entities
{
    public class StockItem : BaseEntity
    {
        public required Guid ProductId { get; init; }
        public required Guid WarehouseId { get; init; }
        public required Guid BatchId { get; init; }

        public int Quantity { get; private set; }
        public decimal Price { get; private set; }

        private StockItem() { }

        public static StockItem Create(Guid productId, Guid warehouseId, Guid batchId, int quantity, decimal price)
        {
            EnsureNonNegativeOrZero(nameof(quantity), quantity);
            EnsureNonNegativeOrZero(nameof(price), price);

            return new StockItem
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                WarehouseId = warehouseId,
                BatchId = batchId,
                Quantity = quantity,
                Price = price
            };
        }

        public void ChangePrice(decimal newPrice)
        {
            EnsureNonNegativeOrZero(nameof(newPrice), newPrice);

            Price = newPrice;
        }

        public static Expression<Func<StockItem, bool>> HasSufficientQuantity(Guid stockItemId, int amount)
        {
            return stockItem => stockItem.Id == stockItemId && stockItem.Quantity >= amount;
        }

        public static Expression<Func<StockItem, bool>> MatchesId(Guid stockItemId)
        {
            return stockItem => stockItem.Id == stockItemId;
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