using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Repositories.Interfaces
{
    public interface ISupplierOrderItemRepository : IBaseEntityRepository<SupplierOrderItem>
    {
        Task<List<SupplierOrderItem>> GetBySupplierOrderIdAsync(Guid supplierOrderId, CancellationToken cancellationToken);
        Task<List<SupplierOrderItem>> GetBySupplierOrderIdsAsync(IReadOnlyList<Guid> supplierOrderIds, CancellationToken cancellationToken);
    }
}