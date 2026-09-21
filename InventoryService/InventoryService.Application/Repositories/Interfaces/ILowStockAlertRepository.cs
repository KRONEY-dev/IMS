using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities;
using Shared.Kernel.Database;

namespace InventoryService.Application.Repositories.Interfaces
{
    public interface ILowStockAlertRepository : IBaseEntityRepository<LowStockAlert>
    {
        Task<List<LowStockAlert>> GetAllAsync(CancellationToken cancellationToken);

        IDirectOperation BuildCreateOperation(LowStockAlert alert);

        IDirectOperation BuildResolveOperation(Guid productId, Guid warehouseId);
    }
}