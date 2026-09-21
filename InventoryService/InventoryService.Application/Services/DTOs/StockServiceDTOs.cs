using InventoryService.Domain.Entities;

namespace InventoryService.Application.Services.DTOs
{
    public static class StockServiceDTOs
    {
        public record CreateStockThresholdRequestDTO(Guid ProductId, Guid WarehouseId, int ReorderLevel, int ReorderQuantity);

        public record GetAllStockThresholdsRequestDTO();
        public record GetAllStockThresholdsResponseDTO(IReadOnlyList<StockThresholdDTO> Thresholds);

        public record GetStockThresholdByIdRequestDTO(Guid StockThresholdId);

        public record UpdateStockThresholdRequestDTO(Guid StockThresholdId, int ReorderLevel, int ReorderQuantity);
        public record UpdateStockThresholdResponseDTO();

        public record DeleteStockThresholdRequestDTO(Guid StockThresholdId);
        public record DeleteStockThresholdResponseDTO();

        public record StockThresholdDTO(Guid Id, Guid ProductId, Guid WarehouseId, int ReorderLevel, int ReorderQuantity);

        public record ReceiveStockRequestDTO(Guid ProductId, Guid WarehouseId, int Quantity, decimal Price, Guid? BatchId = null);

        public record GetAllStockItemsRequestDTO();
        public record GetAllStockItemsResponseDTO(IReadOnlyList<StockItemDTO> StockItems);

        public record GetStockItemByIdRequestDTO(Guid StockItemId);

        public record SellStockRequestDTO(Guid StockItemId, int Quantity);
        public record SellStockResponseDTO();

        public record AdjustStockRequestDTO(Guid StockItemId, int Quantity);
        public record AdjustStockResponseDTO();

        public record StockItemDTO(Guid Id, Guid ProductId, Guid WarehouseId, Guid BatchId, int Quantity, decimal Price);

        public record GetMovementsByStockItemIdRequestDTO(Guid StockItemId);
        public record GetMovementsByStockItemIdResponseDTO(IReadOnlyList<StockMovementDTO> Movements);

        public record StockMovementDTO(Guid Id, Guid StockItemId, Guid ProductId, Guid WarehouseId, Guid BatchId,
            decimal Price, StockMovementType Type, int Quantity, Guid? Reference,
            Guid InitiatedByUserId, Guid PerformedByUserId, DateTime CreatedAt);
    }
}