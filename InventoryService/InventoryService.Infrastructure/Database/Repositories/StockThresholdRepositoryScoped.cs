using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories
{
    public class StockThresholdRepositoryScoped(InventoryDbContext dbContext) :
        BaseEntityRepository<StockThreshold, InventoryDbContext>(dbContext), IStockThresholdRepository
    {
        protected override DbSet<StockThreshold> MainTable => DbContext.StockThresholds;

        public Task<List<StockThreshold>> GetAllAsync(CancellationToken cancellationToken)
        {
            return MainTable.ToListAsync(cancellationToken);
        }

        public Task<bool> ExistsByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken)
        {
            return MainTable.AnyAsync(threshold =>
                threshold.ProductId == productId && threshold.WarehouseId == warehouseId, cancellationToken);
        }

        public Task<StockThreshold?> GetByProductAndWarehouseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken)
        {
            return MainTable.FirstOrDefaultAsync(threshold =>
                threshold.ProductId == productId && threshold.WarehouseId == warehouseId, cancellationToken);
        }
    }
}