using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Services.Interfaces
{
    public interface ILowStockAlertService
    {
        Task<LowStockAlertServiceDTOs.GetAllLowStockAlertsResponseDTO> GetAllAsync(
            LowStockAlertServiceDTOs.GetAllLowStockAlertsRequestDTO request, CancellationToken cancellationToken);

        Task<LowStockAlertServiceDTOs.LowStockAlertDTO> GetByIdAsync(
            LowStockAlertServiceDTOs.GetLowStockAlertByIdRequestDTO request, CancellationToken cancellationToken);

        Task<LowStockAlertServiceDTOs.ResolveLowStockAlertResponseDTO> ResolveAsync(
            LowStockAlertServiceDTOs.ResolveLowStockAlertRequestDTO request, CancellationToken cancellationToken);

        Task EvaluateAfterDecreaseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken);

        Task EvaluateAfterIncreaseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken);
    }
}