using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Services.Interfaces
{
    public interface IStockTransferService
    {
        Task<StockTransferServiceDTOs.InitiateTransferResponseDTO> InitiateAsync(
            StockTransferServiceDTOs.InitiateTransferRequestDTO request, CancellationToken cancellationToken);

        Task<StockTransferServiceDTOs.GetShipmentByIdResponseDTO> GetShipmentByIdAsync(
            StockTransferServiceDTOs.GetShipmentByIdRequestDTO request, CancellationToken cancellationToken);

        Task<StockTransferServiceDTOs.StockTransferDTO> GetByIdAsync(
            StockTransferServiceDTOs.GetStockTransferByIdRequestDTO request, CancellationToken cancellationToken);

        Task<StockTransferServiceDTOs.ReceiveTransferResponseDTO> ReceiveAsync(
            StockTransferServiceDTOs.ReceiveTransferRequestDTO request, CancellationToken cancellationToken);

        Task<StockTransferServiceDTOs.CancelTransferResponseDTO> CancelAsync(
            StockTransferServiceDTOs.CancelTransferRequestDTO request, CancellationToken cancellationToken);
    }
}