using InventoryService.Domain.Entities.Base;
using System.Linq.Expressions;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Domain.Entities
{
    public enum StockTransferStatus
    {
        InTransit,
        Completed,
        Cancelled
    }

    public class StockTransfer : BaseEntity
    {
        public required Guid ShipmentId { get; init; }

        public required Guid ProductId { get; init; }
        public required Guid SourceWarehouseId { get; init; }
        public required Guid DestinationWarehouseId { get; init; }
        public required Guid BatchId { get; init; }

        public required int Quantity { get; init; }
        public required decimal Price { get; init; }

        public StockTransferStatus Status { get; private set; }

        public required Guid InitiatedByUserId { get; init; }
        public Guid? PerformedByUserId { get; private set; }

        public required DateTime CreatedAt { get; init; }
        public required DateTime ExpectedReceiptDate { get; init; }
        public DateTime? CompletedAt { get; private set; }

        private StockTransfer() { }

        public static StockTransfer Create(Guid shipmentId, Guid productId, Guid sourceWarehouseId,
            Guid destinationWarehouseId, Guid batchId, int quantity, decimal price, Guid initiatedByUserId,
            DateTime expectedReceiptDate)
        {
            EnsureNonNegative(nameof(quantity), quantity);

            return new StockTransfer
            {
                Id = Guid.NewGuid(),
                ShipmentId = shipmentId,
                ProductId = productId,
                SourceWarehouseId = sourceWarehouseId,
                DestinationWarehouseId = destinationWarehouseId,
                BatchId = batchId,
                Quantity = quantity,
                Price = price,
                Status = StockTransferStatus.InTransit,
                InitiatedByUserId = initiatedByUserId,
                CreatedAt = DateTime.UtcNow,
                ExpectedReceiptDate = expectedReceiptDate
            };
        }

        public static Expression<Func<StockTransfer, bool>> IsInTransit(Guid transferId)
        {
            return transfer => transfer.Id == transferId && transfer.Status == StockTransferStatus.InTransit;
        }

        public readonly record struct StatusTransition(StockTransferStatus Status, Guid PerformedByUserId, DateTime PerformedAt);

        public static StatusTransition CompletionTransition(Guid performedByUserId)
        {
            return new StatusTransition(StockTransferStatus.Completed, performedByUserId, DateTime.UtcNow);
        }

        public static StatusTransition CancellationTransition(Guid performedByUserId)
        {
            return new StatusTransition(StockTransferStatus.Cancelled, performedByUserId, DateTime.UtcNow);
        }

        private static void EnsureNonNegative(string fieldName, decimal value)
        {
            if (value < 0)
            {
                throw new NegativeValueException(fieldName, value);
            }
        }
    }
}