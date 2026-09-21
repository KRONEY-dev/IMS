using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories
{
    public class SupplierOrderItemRepositoryScoped(InventoryDbContext dbContext) :
        BaseEntityRepository<SupplierOrderItem, InventoryDbContext>(dbContext), ISupplierOrderItemRepository
    {
        protected override DbSet<SupplierOrderItem> MainTable => DbContext.SupplierOrderItems;

        public Task<List<SupplierOrderItem>> GetBySupplierOrderIdAsync(Guid supplierOrderId, CancellationToken cancellationToken)
        {
            return MainTable.Where(item => item.SupplierOrderId == supplierOrderId).ToListAsync(cancellationToken);
        }
    }
}