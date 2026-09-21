using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities;
using Shared.Kernel.Database;

namespace InventoryService.Application.Repositories.Interfaces
{
    public interface IStockItemRepository : IBaseEntityRepository<StockItem>
    {
        Task<List<StockItem>> GetAllAsync(CancellationToken cancellationToken);

        Task<StockItem?> GetByWarehouseAndBatchIdAsync(
            Guid warehouseId, Guid productId, Guid batchId, CancellationToken cancellationToken);

        Task<int> GetTotalQuantityAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken);

        IDirectOperation BuildDecrementOperation(Guid stockItemId, int amount);

        IDirectOperation BuildIncrementOperation(Guid stockItemId, int amount);

        IDirectOperation BuildCreateOperation(StockItem stockItem);
    }
}