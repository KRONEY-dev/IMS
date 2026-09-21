using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Repositories.Interfaces
{
    public interface IStockThresholdRepository : IBaseEntityRepository<StockThreshold>
    {
        Task<List<StockThreshold>> GetAllAsync(CancellationToken cancellationToken);

        Task<bool> ExistsByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken);

        Task<StockThreshold?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken);
    }
}