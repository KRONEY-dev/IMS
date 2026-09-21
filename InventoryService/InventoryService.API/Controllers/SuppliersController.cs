using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Shared.Kernel.AspNetCore;

namespace InventoryService.API.Controllers
{
    [ApiController]
    [Route("api/[controller]/[action]")]
    [Authorize]
    public class SuppliersController(ISupplierService supplierService) : BaseController<ISupplierService>(supplierService)
    {
        [HttpPost]
        public async Task<ActionResult<SupplierServiceDTOs.SupplierDTO>> Create(
            SupplierServiceDTOs.CreateSupplierRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.CreateAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<SupplierServiceDTOs.GetAllSuppliersResponseDTO>> GetAll(
            SupplierServiceDTOs.GetAllSuppliersRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetAllAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<SupplierServiceDTOs.SupplierDTO>> GetById(
            SupplierServiceDTOs.GetSupplierByIdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetByIdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<SupplierServiceDTOs.UpdateSupplierResponseDTO>> Update(
            SupplierServiceDTOs.UpdateSupplierRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.UpdateAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<SupplierServiceDTOs.DeleteSupplierResponseDTO>> Delete(
            SupplierServiceDTOs.DeleteSupplierRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.DeleteAsync(request, cancellationToken));
        }
    }
}