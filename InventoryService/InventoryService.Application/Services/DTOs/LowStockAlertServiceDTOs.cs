using InventoryService.Domain.Entities;

namespace InventoryService.Application.Services.DTOs
{
    public static class LowStockAlertServiceDTOs
    {
        public record GetAllLowStockAlertsRequestDTO();
        public record GetAllLowStockAlertsResponseDTO(IReadOnlyList<LowStockAlertDTO> Alerts);

        public record GetLowStockAlertByIdRequestDTO(Guid LowStockAlertId);

        public record ResolveLowStockAlertRequestDTO(Guid LowStockAlertId);
        public record ResolveLowStockAlertResponseDTO();

        public record LowStockAlertDTO(Guid Id, Guid ProductId, Guid WarehouseId, LowStockAlertStatus Status,
            DateTime CreatedAt, DateTime? ResolvedAt);
    }
}