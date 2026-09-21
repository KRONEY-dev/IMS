using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Repositories.Interfaces
{
    public interface IProductRepository : IBaseEntityRepository<Product>
    {
        Task<List<Product>> GetAllAsync(CancellationToken cancellationToken);
    }
}