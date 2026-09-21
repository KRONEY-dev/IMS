using InventoryService.Domain.Entities;

namespace InventoryService.Application.Services.DTOs
{
    public static class SupplierOrderServiceDTOs
    {
        public record CreateSupplierOrderItemDTO(Guid ProductId, int Quantity, decimal PurchasePrice);
        public record CreateSupplierOrderRequestDTO(
            Guid SupplierId, Guid WarehouseId, IReadOnlyList<CreateSupplierOrderItemDTO> Items);

        public record GetAllSupplierOrdersRequestDTO();
        public record GetAllSupplierOrdersResponseDTO(IReadOnlyList<SupplierOrderDTO> Orders);

        public record GetSupplierOrderByIdRequestDTO(Guid SupplierOrderId);

        public record SubmitSupplierOrderRequestDTO(Guid SupplierOrderId);
        public record SubmitSupplierOrderResponseDTO();

        public record ReceiveSupplierOrderItemDTO(Guid SupplierOrderItemId, decimal SalePrice);
        public record ReceiveSupplierOrderRequestDTO(Guid SupplierOrderId, IReadOnlyList<ReceiveSupplierOrderItemDTO> Items);
        public record ReceiveSupplierOrderResponseDTO(IReadOnlyList<StockServiceDTOs.StockItemDTO> CreatedStockItems);

        public record SupplierOrderItemDTO(Guid Id, Guid ProductId, int Quantity, decimal PurchasePrice);

        public record SupplierOrderDTO(Guid Id, Guid SupplierId, Guid WarehouseId, SupplierOrderStatus Status,
            Guid CreatedByUserId, DateTime CreatedAt, DateTime? SubmittedAt, DateTime? ReceivedAt,
            IReadOnlyList<SupplierOrderItemDTO> Items);
    }
}