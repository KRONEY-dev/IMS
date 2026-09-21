using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories
{
    public class ProductRepositoryScoped(InventoryDbContext dbContext) :
        BaseEntityRepository<Product, InventoryDbContext>(dbContext), IProductRepository
    {
        protected override DbSet<Product> MainTable => DbContext.Products;

        public Task<List<Product>> GetAllAsync(CancellationToken cancellationToken)
        {
            return MainTable.ToListAsync(cancellationToken);
        }
    }
}