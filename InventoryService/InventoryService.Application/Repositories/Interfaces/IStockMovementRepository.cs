using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities;
using Shared.Kernel.Database;

namespace InventoryService.Application.Repositories.Interfaces
{
    public interface IStockMovementRepository : IBaseEntityRepository<StockMovement>
    {
        Task<List<StockMovement>> GetByStockItemIdAsync(Guid stockItemId, CancellationToken cancellationToken);

        IDirectOperation BuildCreateOperation(StockMovement movement);
    }
}