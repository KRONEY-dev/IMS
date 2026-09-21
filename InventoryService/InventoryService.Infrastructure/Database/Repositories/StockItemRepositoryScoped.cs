using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Database;
using Shared.Kernel.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories
{
    public class StockItemRepositoryScoped(InventoryDbContext dbContext) :
        BaseEntityRepository<StockItem, InventoryDbContext>(dbContext), IStockItemRepository
    {
        protected override DbSet<StockItem> MainTable => DbContext.StockItems;

        public Task<List<StockItem>> GetAllAsync(CancellationToken cancellationToken)
        {
            return MainTable.ToListAsync(cancellationToken);
        }

        public Task<StockItem?> GetByWarehouseAndBatchIdAsync(
            Guid warehouseId, Guid productId, Guid batchId, CancellationToken cancellationToken)
        {
            return MainTable.FirstOrDefaultAsync(item =>
                item.WarehouseId == warehouseId && item.ProductId == productId && item.BatchId == batchId,
                cancellationToken);
        }

        public Task<int> GetTotalQuantityAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken)
        {
            return MainTable
                .Where(item => item.ProductId == productId && item.WarehouseId == warehouseId)
                .SumAsync(item => item.Quantity, cancellationToken);
        }

        public IDirectOperation BuildDecrementOperation(Guid stockItemId, int amount)
        {
            return new DirectUpdate<StockItem>(
                StockItem.HasSufficientQuantity(stockItemId, amount),
                setters => setters.SetProperty(item => item.Quantity, item => item.Quantity - amount));
        }

        public IDirectOperation BuildIncrementOperation(Guid stockItemId, int amount)
        {
            return new DirectUpdate<StockItem>(
                StockItem.MatchesId(stockItemId),
                setters => setters.SetProperty(item => item.Quantity, item => item.Quantity + amount));
        }

        public IDirectOperation BuildCreateOperation(StockItem stockItem)
        {
            return new DirectInsert<StockItem>(stockItem);
        }
    }
}