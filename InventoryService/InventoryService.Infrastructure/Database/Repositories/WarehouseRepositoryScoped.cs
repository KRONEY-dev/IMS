using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories
{
    public class WarehouseRepositoryScoped(InventoryDbContext dbContext) :
        BaseEntityRepository<Warehouse, InventoryDbContext>(dbContext), IWarehouseRepository
    {
        protected override DbSet<Warehouse> MainTable => DbContext.Warehouses;

        public Task<List<Warehouse>> GetAllAsync(CancellationToken cancellationToken)
        {
            return MainTable.ToListAsync(cancellationToken);
        }

        public Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken)
        {
            return MainTable.AnyAsync(warehouse => warehouse.Name == name, cancellationToken);
        }
    }
}