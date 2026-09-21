using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Services.Interfaces
{
    public interface IStockService
    {
        Task<StockServiceDTOs.StockThresholdDTO> CreateThresholdAsync(
            StockServiceDTOs.CreateStockThresholdRequestDTO request, CancellationToken cancellationToken);

        Task<StockServiceDTOs.GetAllStockThresholdsResponseDTO> GetAllThresholdsAsync(
            StockServiceDTOs.GetAllStockThresholdsRequestDTO request, CancellationToken cancellationToken);

        Task<StockServiceDTOs.StockThresholdDTO> GetThresholdByIdAsync(
            StockServiceDTOs.GetStockThresholdByIdRequestDTO request, CancellationToken cancellationToken);

        Task<StockServiceDTOs.UpdateStockThresholdResponseDTO> UpdateThresholdAsync(
            StockServiceDTOs.UpdateStockThresholdRequestDTO request, CancellationToken cancellationToken);

        Task<StockServiceDTOs.DeleteStockThresholdResponseDTO> DeleteThresholdAsync(
            StockServiceDTOs.DeleteStockThresholdRequestDTO request, CancellationToken cancellationToken);

        Task<StockServiceDTOs.StockItemDTO> ReceiveAsync(
            StockServiceDTOs.ReceiveStockRequestDTO request, CancellationToken cancellationToken);

        Task<StockServiceDTOs.GetAllStockItemsResponseDTO> GetAllStockItemsAsync(
            StockServiceDTOs.GetAllStockItemsRequestDTO request, CancellationToken cancellationToken);

        Task<StockServiceDTOs.StockItemDTO> GetStockItemByIdAsync(
            StockServiceDTOs.GetStockItemByIdRequestDTO request, CancellationToken cancellationToken);

        Task<StockServiceDTOs.SellStockResponseDTO> SellAsync(
            StockServiceDTOs.SellStockRequestDTO request, CancellationToken cancellationToken);

        Task<StockServiceDTOs.AdjustStockResponseDTO> AdjustAsync(
            StockServiceDTOs.AdjustStockRequestDTO request, CancellationToken cancellationToken);

        Task<StockServiceDTOs.GetMovementsByStockItemIdResponseDTO> GetMovementsByStockItemIdAsync(
            StockServiceDTOs.GetMovementsByStockItemIdRequestDTO request, CancellationToken cancellationToken);
    }
}