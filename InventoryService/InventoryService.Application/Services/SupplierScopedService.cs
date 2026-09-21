using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Shared.Kernel.Services;

namespace InventoryService.Application.Services
{
    public class SupplierScopedService : BaseService, ISupplierService
    {
        private readonly ISupplierRepository _supplierRepository;

        public SupplierScopedService(IUnitOfWork unitOfWork, IRequestContext requestContext,
            ISupplierRepository supplierRepository, IMapperWrapper mapper) : base(unitOfWork, requestContext, mapper)
        {
            _supplierRepository = supplierRepository;
        }

        public async Task<SupplierServiceDTOs.SupplierDTO> CreateAsync(
            SupplierServiceDTOs.CreateSupplierRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var supplier = Supplier.Create(request.Name, request.ContactEmail, request.ContactPhone);

            _supplierRepository.Add(supplier);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return Mapper.Map<SupplierServiceDTOs.SupplierDTO>(supplier);
        }

        public async Task<SupplierServiceDTOs.GetAllSuppliersResponseDTO> GetAllAsync(
            SupplierServiceDTOs.GetAllSuppliersRequestDTO request, CancellationToken cancellationToken)
        {
            var suppliers = await _supplierRepository.GetAllAsync(cancellationToken);

            return new SupplierServiceDTOs.GetAllSuppliersResponseDTO(
                Mapper.Map<List<SupplierServiceDTOs.SupplierDTO>>(suppliers));
        }

        public async Task<SupplierServiceDTOs.SupplierDTO> GetByIdAsync(
            SupplierServiceDTOs.GetSupplierByIdRequestDTO request, CancellationToken cancellationToken)
        {
            var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
                ?? throw new NotFoundException(nameof(Supplier), request.SupplierId);

            return Mapper.Map<SupplierServiceDTOs.SupplierDTO>(supplier);
        }

        public async Task<SupplierServiceDTOs.UpdateSupplierResponseDTO> UpdateAsync(
            SupplierServiceDTOs.UpdateSupplierRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
                ?? throw new NotFoundException(nameof(Supplier), request.SupplierId);

            supplier.Rename(request.Name);
            supplier.UpdateContactEmail(request.ContactEmail);
            supplier.UpdateContactPhone(request.ContactPhone);

            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new SupplierServiceDTOs.UpdateSupplierResponseDTO();
        }

        public async Task<SupplierServiceDTOs.DeleteSupplierResponseDTO> DeleteAsync(
            SupplierServiceDTOs.DeleteSupplierRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var supplier = await _supplierRepository.GetByIdAsync(request.SupplierId, cancellationToken)
                ?? throw new NotFoundException(nameof(Supplier), request.SupplierId);

            _supplierRepository.Remove(supplier);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new SupplierServiceDTOs.DeleteSupplierResponseDTO();
        }
    }
}