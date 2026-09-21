using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Services.Interfaces
{
    public interface ISupplierService
    {
        Task<SupplierServiceDTOs.SupplierDTO> CreateAsync(
            SupplierServiceDTOs.CreateSupplierRequestDTO request, CancellationToken cancellationToken);

        Task<SupplierServiceDTOs.GetAllSuppliersResponseDTO> GetAllAsync(
            SupplierServiceDTOs.GetAllSuppliersRequestDTO request, CancellationToken cancellationToken);

        Task<SupplierServiceDTOs.SupplierDTO> GetByIdAsync(
            SupplierServiceDTOs.GetSupplierByIdRequestDTO request, CancellationToken cancellationToken);

        Task<SupplierServiceDTOs.UpdateSupplierResponseDTO> UpdateAsync(
            SupplierServiceDTOs.UpdateSupplierRequestDTO request, CancellationToken cancellationToken);

        Task<SupplierServiceDTOs.DeleteSupplierResponseDTO> DeleteAsync(
            SupplierServiceDTOs.DeleteSupplierRequestDTO request, CancellationToken cancellationToken);
    }
}