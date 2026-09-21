using InventoryService.Domain.Entities.Base;
using System.Linq.Expressions;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Domain.Entities
{
    public enum LowStockAlertStatus
    {
        Active,
        Resolved
    }

    public class LowStockAlert : BaseEntity
    {
        public required Guid ProductId { get; init; }
        public required Guid WarehouseId { get; init; }

        public LowStockAlertStatus Status { get; private set; }

        public required DateTime CreatedAt { get; init; }
        public DateTime? ResolvedAt { get; private set; }

        private LowStockAlert() { }

        public static LowStockAlert Create(Guid productId, Guid warehouseId)
        {
            return new LowStockAlert
            {
                Id = Guid.NewGuid(),
                ProductId = productId,
                WarehouseId = warehouseId,
                Status = LowStockAlertStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void Resolve()
        {
            if (Status != LowStockAlertStatus.Active)
            {
                throw new LowStockAlertAlreadyResolvedException(Id);
            }

            Status = LowStockAlertStatus.Resolved;
            ResolvedAt = DateTime.UtcNow;
        }

        public static Expression<Func<LowStockAlert, bool>> IsActiveFor(Guid productId, Guid warehouseId)
        {
            return alert => alert.ProductId == productId && alert.WarehouseId == warehouseId
                && alert.Status == LowStockAlertStatus.Active;
        }

        public readonly record struct ResolutionTransition(LowStockAlertStatus Status, DateTime ResolvedAt);

        public static ResolutionTransition AutoResolveTransition()
        {
            return new ResolutionTransition(LowStockAlertStatus.Resolved, DateTime.UtcNow);
        }
    }
}