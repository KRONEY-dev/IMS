using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Database;
using Shared.Kernel.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories
{
    public class StockMovementRepositoryScoped(InventoryDbContext dbContext) :
        BaseEntityRepository<StockMovement, InventoryDbContext>(dbContext), IStockMovementRepository
    {
        protected override DbSet<StockMovement> MainTable => DbContext.StockMovements;

        public Task<List<StockMovement>> GetByStockItemIdAsync(Guid stockItemId, CancellationToken cancellationToken)
        {
            return MainTable.Where(movement => movement.StockItemId == stockItemId).ToListAsync(cancellationToken);
        }

        public IDirectOperation BuildCreateOperation(StockMovement movement)
        {
            return new DirectInsert<StockMovement>(movement);
        }
    }
}