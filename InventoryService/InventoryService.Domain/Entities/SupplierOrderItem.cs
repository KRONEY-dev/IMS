using InventoryService.Domain.Entities.Base;
using System.Linq.Expressions;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Domain.Entities
{
    public class SupplierOrderItem : BaseEntity
    {
        public required Guid SupplierOrderId { get; init; }
        public required Guid ProductId { get; init; }

        public int Quantity { get; private set; }
        public decimal PurchasePrice { get; private set; }

        private SupplierOrderItem() { }

        public static SupplierOrderItem Create(Guid supplierOrderId, Guid productId, int quantity, decimal purchasePrice)
        {
            EnsureNonNegative(nameof(quantity), quantity);
            EnsureNonNegative(nameof(purchasePrice), purchasePrice);

            return new SupplierOrderItem
            {
                Id = Guid.NewGuid(),
                SupplierOrderId = supplierOrderId,
                ProductId = productId,
                Quantity = quantity,
                PurchasePrice = purchasePrice
            };
        }

        public static Expression<Func<SupplierOrderItem, bool>> BelongsToOrders(IReadOnlyList<Guid> supplierOrderIds)
        {
            return item => supplierOrderIds.Contains(item.SupplierOrderId);
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