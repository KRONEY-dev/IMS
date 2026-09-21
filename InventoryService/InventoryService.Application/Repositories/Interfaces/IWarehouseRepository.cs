using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Repositories.Interfaces
{
    public interface IWarehouseRepository : IBaseEntityRepository<Warehouse>
    {
        Task<List<Warehouse>> GetAllAsync(CancellationToken cancellationToken);

        Task<bool> ExistsByNameAsync(string name, CancellationToken cancellationToken);
    }
}