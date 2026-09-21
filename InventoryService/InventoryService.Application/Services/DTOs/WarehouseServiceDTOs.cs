using InventoryService.Domain.Entities;

namespace InventoryService.Application.Services.DTOs
{
    public static class WarehouseServiceDTOs
    {
        public record CreateWarehouseRequestDTO(string Name, string Location, IReadOnlyList<WorkingHoursDTO> WorkingHours);

        public record GetAllWarehousesRequestDTO();
        public record GetAllWarehousesResponseDTO(IReadOnlyList<WarehouseDTO> Warehouses);

        public record GetWarehouseByIdRequestDTO(Guid WarehouseId);

        public record UpdateWarehouseStatusRequestDTO(Guid WarehouseId, WarehouseStatus Status);
        public record UpdateWarehouseStatusResponseDTO();

        public record UpdateWarehouseWorkingHoursRequestDTO(Guid WarehouseId, IReadOnlyList<WorkingHoursDTO> WorkingHours);
        public record UpdateWarehouseWorkingHoursResponseDTO();

        public record DeleteWarehouseRequestDTO(Guid WarehouseId);
        public record DeleteWarehouseResponseDTO();

        public record WarehouseDTO(Guid Id, string Name, string Location, WarehouseStatus Status,
            IReadOnlyList<WorkingHoursDTO> WorkingHours);

        public record WorkingHoursDTO(DayOfWeek DayOfWeek, bool IsClosed, TimeOnly OpensAt, TimeOnly ClosesAt);
    }
}