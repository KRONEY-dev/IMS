using InventoryService.Domain.Entities.Base;
using System.Linq.Expressions;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Domain.Entities
{
    public enum SupplierOrderStatus
    {
        Created,
        Submitted,
        Received
    }

    public class SupplierOrder : BaseEntity
    {
        public required Guid SupplierId { get; init; }
        public required Guid WarehouseId { get; init; }

        public SupplierOrderStatus Status { get; private set; }

        public required Guid CreatedByUserId { get; init; }

        public required DateTime CreatedAt { get; init; }
        public DateTime? SubmittedAt { get; private set; }
        public DateTime? ReceivedAt { get; private set; }

        private SupplierOrder() { }

        public static SupplierOrder Create(Guid supplierId, Guid warehouseId, Guid createdByUserId)
        {
            return new SupplierOrder
            {
                Id = Guid.NewGuid(),
                SupplierId = supplierId,
                WarehouseId = warehouseId,
                Status = SupplierOrderStatus.Created,
                CreatedByUserId = createdByUserId,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Submit()
        {
            if (Status != SupplierOrderStatus.Created)
            {
                throw new SupplierOrderNotCreatedException(Id);
            }

            Status = SupplierOrderStatus.Submitted;
            SubmittedAt = DateTime.UtcNow;
        }

        public static Expression<Func<SupplierOrder, bool>> IsSubmitted(Guid orderId)
        {
            return order => order.Id == orderId && order.Status == SupplierOrderStatus.Submitted;
        }

        public readonly record struct ReceiptTransition(SupplierOrderStatus Status, DateTime ReceivedAt);

        public static ReceiptTransition ReceiveTransition()
        {
            return new ReceiptTransition(SupplierOrderStatus.Received, DateTime.UtcNow);
        }
    }
}