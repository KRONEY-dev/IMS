using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Services.Interfaces
{
    public interface IWarehouseService
    {
        Task<WarehouseServiceDTOs.WarehouseDTO> CreateAsync(
            WarehouseServiceDTOs.CreateWarehouseRequestDTO request, CancellationToken cancellationToken);

        Task<WarehouseServiceDTOs.GetAllWarehousesResponseDTO> GetAllAsync(
            WarehouseServiceDTOs.GetAllWarehousesRequestDTO request, CancellationToken cancellationToken);

        Task<WarehouseServiceDTOs.WarehouseDTO> GetByIdAsync(
            WarehouseServiceDTOs.GetWarehouseByIdRequestDTO request, CancellationToken cancellationToken);

        Task<WarehouseServiceDTOs.UpdateWarehouseStatusResponseDTO> UpdateStatusAsync(
            WarehouseServiceDTOs.UpdateWarehouseStatusRequestDTO request, CancellationToken cancellationToken);

        Task<WarehouseServiceDTOs.UpdateWarehouseWorkingHoursResponseDTO> UpdateWorkingHoursAsync(
            WarehouseServiceDTOs.UpdateWarehouseWorkingHoursRequestDTO request, CancellationToken cancellationToken);

        Task<WarehouseServiceDTOs.DeleteWarehouseResponseDTO> DeleteAsync(
            WarehouseServiceDTOs.DeleteWarehouseRequestDTO request, CancellationToken cancellationToken);
    }
}