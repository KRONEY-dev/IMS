using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Entities.ValueObjects;
using InventoryService.Domain.Enums;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Shared.Kernel.Services;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Application.Services
{
    public class WarehouseScopedService : BaseService, IWarehouseService
    {
        private readonly IWarehouseRepository _warehouseRepository;

        public WarehouseScopedService(IUnitOfWork unitOfWork, IRequestContext requestContext,
            IWarehouseRepository warehouseRepository, IMapperWrapper mapper) : base(unitOfWork, requestContext, mapper)
        {
            _warehouseRepository = warehouseRepository;
        }

        public async Task<WarehouseServiceDTOs.WarehouseDTO> CreateAsync(
            WarehouseServiceDTOs.CreateWarehouseRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Admin);

            if (await _warehouseRepository.ExistsByNameAsync(request.Name, cancellationToken))
            {
                throw new WarehouseNameAlreadyTakenException(request.Name);
            }

            var workingHoursFromDto = Mapper.Map<List<WorkingHours>>(request.WorkingHours);
            var warehouse = Warehouse.Create(request.Name, request.Location, workingHoursFromDto);

            _warehouseRepository.Add(warehouse);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return Mapper.Map<WarehouseServiceDTOs.WarehouseDTO>(warehouse);
        }

        public async Task<WarehouseServiceDTOs.GetAllWarehousesResponseDTO> GetAllAsync(
            WarehouseServiceDTOs.GetAllWarehousesRequestDTO request, CancellationToken cancellationToken)
        {
            var warehouses = await _warehouseRepository.GetAllAsync(cancellationToken);

            return new WarehouseServiceDTOs.GetAllWarehousesResponseDTO(
                Mapper.Map<List<WarehouseServiceDTOs.WarehouseDTO>>(warehouses));
        }

        public async Task<WarehouseServiceDTOs.WarehouseDTO> GetByIdAsync(
            WarehouseServiceDTOs.GetWarehouseByIdRequestDTO request, CancellationToken cancellationToken)
        {
            var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken)
                ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

            return Mapper.Map<WarehouseServiceDTOs.WarehouseDTO>(warehouse);
        }

        public async Task<WarehouseServiceDTOs.UpdateWarehouseStatusResponseDTO> UpdateStatusAsync(
            WarehouseServiceDTOs.UpdateWarehouseStatusRequestDTO request, CancellationToken cancellationToken)
        {
            EnsureCanManageWarehouse(request.WarehouseId);

            var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken)
                ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

            warehouse.ChangeStatus(request.Status);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new WarehouseServiceDTOs.UpdateWarehouseStatusResponseDTO();
        }

        public async Task<WarehouseServiceDTOs.UpdateWarehouseWorkingHoursResponseDTO> UpdateWorkingHoursAsync(
            WarehouseServiceDTOs.UpdateWarehouseWorkingHoursRequestDTO request, CancellationToken cancellationToken)
        {
            EnsureCanManageWarehouse(request.WarehouseId);

            var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken)
                ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

            warehouse.UpdateWorkingHours(Mapper.Map<List<WorkingHours>>(request.WorkingHours));
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new WarehouseServiceDTOs.UpdateWarehouseWorkingHoursResponseDTO();
        }

        public async Task<WarehouseServiceDTOs.DeleteWarehouseResponseDTO> DeleteAsync(
            WarehouseServiceDTOs.DeleteWarehouseRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Admin);

            var warehouse = await _warehouseRepository.GetByIdAsync(request.WarehouseId, cancellationToken)
                ?? throw new NotFoundException(nameof(Warehouse), request.WarehouseId);

            _warehouseRepository.Remove(warehouse);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new WarehouseServiceDTOs.DeleteWarehouseResponseDTO();
        }

        private void EnsureCanManageWarehouse(Guid warehouseId)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);
            RequestContext.EnsureWarehouseAccess(warehouseId, UserRole.Admin);
        }
    }
}