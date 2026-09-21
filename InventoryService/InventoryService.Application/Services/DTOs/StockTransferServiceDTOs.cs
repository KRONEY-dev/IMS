using InventoryService.Domain.Entities;

namespace InventoryService.Application.Services.DTOs
{
    public static class StockTransferServiceDTOs
    {
        public record StockTransferLineDTO(Guid StockItemId, Guid BatchId, int Quantity);

        public record InitiateTransferRequestDTO(
            Guid DestinationWarehouseId, DateTime ExpectedReceiptDate, IReadOnlyList<StockTransferLineDTO> Items);
        public record InitiateTransferResponseDTO(Guid ShipmentId, IReadOnlyList<StockTransferDTO> Transfers);

        public record GetShipmentByIdRequestDTO(Guid ShipmentId);
        public record GetShipmentByIdResponseDTO(IReadOnlyList<StockTransferDTO> Transfers);

        public record GetStockTransferByIdRequestDTO(Guid StockTransferId);

        public record CompleteTransferRequestDTO(Guid StockTransferId);
        public record CompleteTransferResponseDTO();

        public record CancelTransferRequestDTO(Guid StockTransferId);
        public record CancelTransferResponseDTO();

        public record StockTransferDTO(Guid Id, Guid ShipmentId, Guid ProductId, Guid SourceWarehouseId,
            Guid DestinationWarehouseId, Guid BatchId, int Quantity, decimal Price, StockTransferStatus Status,
            Guid InitiatedByUserId, Guid? PerformedByUserId, DateTime CreatedAt, DateTime ExpectedReceiptDate,
            DateTime? CompletedAt);
    }
}