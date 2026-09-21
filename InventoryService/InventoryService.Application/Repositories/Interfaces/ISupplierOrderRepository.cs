using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities;
using Shared.Kernel.Database;

namespace InventoryService.Application.Repositories.Interfaces
{
    public interface ISupplierOrderRepository : IBaseEntityRepository<SupplierOrder>
    {
        Task<List<SupplierOrder>> GetAllAsync(CancellationToken cancellationToken);

        Task<List<SupplierOrder>> GetArrivingByAsync(DateTime cutoff, CancellationToken cancellationToken);

        IDirectOperation BuildReceiveOperation(Guid orderId);
    }
}