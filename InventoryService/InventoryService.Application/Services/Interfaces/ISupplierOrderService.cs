using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Services.Interfaces
{
    public interface ISupplierOrderService
    {
        Task<SupplierOrderServiceDTOs.SupplierOrderDTO> CreateAsync(
            SupplierOrderServiceDTOs.CreateSupplierOrderRequestDTO request, CancellationToken cancellationToken);

        Task<SupplierOrderServiceDTOs.GetAllSupplierOrdersResponseDTO> GetAllAsync(
            SupplierOrderServiceDTOs.GetAllSupplierOrdersRequestDTO request, CancellationToken cancellationToken);

        Task<SupplierOrderServiceDTOs.SupplierOrderDTO> GetByIdAsync(
            SupplierOrderServiceDTOs.GetSupplierOrderByIdRequestDTO request, CancellationToken cancellationToken);

        Task<SupplierOrderServiceDTOs.SubmitSupplierOrderResponseDTO> SubmitAsync(
            SupplierOrderServiceDTOs.SubmitSupplierOrderRequestDTO request, CancellationToken cancellationToken);

        Task<SupplierOrderServiceDTOs.ReceiveSupplierOrderResponseDTO> ReceiveAsync(
            SupplierOrderServiceDTOs.ReceiveSupplierOrderRequestDTO request, CancellationToken cancellationToken);
    }
}