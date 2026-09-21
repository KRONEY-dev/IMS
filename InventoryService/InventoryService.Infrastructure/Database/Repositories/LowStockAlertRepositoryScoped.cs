using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Database;
using Shared.Kernel.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories
{
    public class LowStockAlertRepositoryScoped(InventoryDbContext dbContext) :
        BaseEntityRepository<LowStockAlert, InventoryDbContext>(dbContext), ILowStockAlertRepository
    {
        protected override DbSet<LowStockAlert> MainTable => DbContext.LowStockAlerts;

        public Task<List<LowStockAlert>> GetAllAsync(CancellationToken cancellationToken)
        {
            return MainTable.ToListAsync(cancellationToken);
        }

        public IDirectOperation BuildCreateOperation(LowStockAlert alert)
        {
            return new DirectInsert<LowStockAlert>(alert);
        }

        public IDirectOperation BuildResolveOperation(Guid productId, Guid warehouseId)
        {
            var transition = LowStockAlert.AutoResolveTransition();

            return new DirectUpdate<LowStockAlert>(
                LowStockAlert.IsActiveFor(productId, warehouseId),
                setters => setters
                    .SetProperty(alert => alert.Status, transition.Status)
                    .SetProperty(alert => alert.ResolvedAt, transition.ResolvedAt));
        }
    }
}