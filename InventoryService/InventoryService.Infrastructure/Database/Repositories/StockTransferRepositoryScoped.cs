using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database.Repositories.Base;
using Microsoft.EntityFrameworkCore;
using Shared.Kernel.Database;
using Shared.Kernel.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database.Repositories
{
    public class StockTransferRepositoryScoped(InventoryDbContext dbContext) :
        BaseEntityRepository<StockTransfer, InventoryDbContext>(dbContext), IStockTransferRepository
    {
        protected override DbSet<StockTransfer> MainTable => DbContext.StockTransfers;

        public Task<List<StockTransfer>> GetByShipmentIdAsync(Guid shipmentId, CancellationToken cancellationToken)
        {
            return MainTable.Where(transfer => transfer.ShipmentId == shipmentId).ToListAsync(cancellationToken);
        }

        public IDirectOperation BuildCreateOperation(StockTransfer transfer)
        {
            return new DirectInsert<StockTransfer>(transfer);
        }

        public IDirectOperation BuildReceiveOperation(Guid transferId, int receivedQuantity, Guid performedByUserId)
        {
            var performedAt = DateTime.UtcNow;

            return new DirectUpdate<StockTransfer>(
                StockTransfer.CanReceive(transferId, receivedQuantity),
                setters => setters
                    .SetProperty(transfer => transfer.Quantity, StockTransfer.RemainingQuantityAfterReceipt(receivedQuantity))
                    .SetProperty(transfer => transfer.Status, StockTransfer.StatusAfterReceipt(receivedQuantity))
                    .SetProperty(transfer => transfer.PerformedByUserId, performedByUserId)
                    .SetProperty(transfer => transfer.CompletedAt, StockTransfer.CompletedAtAfterReceipt(receivedQuantity, performedAt)));
        }

        public IDirectOperation BuildCancelOperation(Guid transferId, Guid performedByUserId)
        {
            return BuildStatusTransitionOperation(transferId, StockTransfer.CancellationTransition(performedByUserId));
        }

        private static IDirectOperation BuildStatusTransitionOperation(Guid transferId, StockTransfer.StatusTransition transition)
        {
            return new DirectUpdate<StockTransfer>(
                StockTransfer.IsInTransit(transferId),
                setters => setters
                    .SetProperty(transfer => transfer.Status, transition.Status)
                    .SetProperty(transfer => transfer.PerformedByUserId, transition.PerformedByUserId)
                    .SetProperty(transfer => transfer.CompletedAt, transition.PerformedAt));
        }
    }
}