using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories
{
    public class SupplierRepositoryScoped(InventoryDbContext dbContext) :
        BaseEntityRepository<Supplier, InventoryDbContext>(dbContext), ISupplierRepository
    {
        protected override DbSet<Supplier> MainTable => DbContext.Suppliers;

        public Task<List<Supplier>> GetAllAsync(CancellationToken cancellationToken)
        {
            return MainTable.ToListAsync(cancellationToken);
        }
    }
}