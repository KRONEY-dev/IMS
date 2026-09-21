using InventoryService.Domain.Entities.Base;
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

        private static void EnsureNonNegative(string fieldName, decimal value)
        {
            if (value < 0)
            {
                throw new NegativeValueException(fieldName, value);
            }
        }
    }
}