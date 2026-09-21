using InventoryService.Application.Repositories.Interfaces.Base;
using InventoryService.Domain.Entities;
using Shared.Kernel.Database;

namespace InventoryService.Application.Repositories.Interfaces
{
    public interface IStockTransferRepository : IBaseEntityRepository<StockTransfer>
    {
        Task<List<StockTransfer>> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken);

        IDirectOperation BuildCreateOperation(StockTransfer transfer);

        IDirectOperation BuildReceiveOperation(Guid transferId, int receivedQuantity, Guid performedByUserId);

        IDirectOperation BuildCancelOperation(Guid transferId, Guid performedByUserId);
    }
}