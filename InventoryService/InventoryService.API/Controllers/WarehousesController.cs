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
    public class WarehousesController(IWarehouseService warehouseService) : BaseController<IWarehouseService>(warehouseService)
    {
        [HttpPost]
        public async Task<ActionResult<WarehouseServiceDTOs.WarehouseDTO>> Create(
            WarehouseServiceDTOs.CreateWarehouseRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.CreateAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<WarehouseServiceDTOs.GetAllWarehousesResponseDTO>> GetAll(
            WarehouseServiceDTOs.GetAllWarehousesRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetAllAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<WarehouseServiceDTOs.WarehouseDTO>> GetById(
            WarehouseServiceDTOs.GetWarehouseByIdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetByIdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<WarehouseServiceDTOs.UpdateWarehouseStatusResponseDTO>> UpdateStatus(
            WarehouseServiceDTOs.UpdateWarehouseStatusRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.UpdateStatusAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<WarehouseServiceDTOs.UpdateWarehouseWorkingHoursResponseDTO>> UpdateWorkingHours(
            WarehouseServiceDTOs.UpdateWarehouseWorkingHoursRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.UpdateWorkingHoursAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<WarehouseServiceDTOs.DeleteWarehouseResponseDTO>> Delete(
            WarehouseServiceDTOs.DeleteWarehouseRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.DeleteAsync(request, cancellationToken));
        }
    }
}