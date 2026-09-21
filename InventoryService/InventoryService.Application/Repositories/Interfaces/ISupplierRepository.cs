using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities;

namespace InventoryService.Application.Repositories.Interfaces
{
    public interface ISupplierRepository : IBaseEntityRepository<Supplier>
    {
        Task<List<Supplier>> GetAllAsync(CancellationToken cancellationToken);
    }
}