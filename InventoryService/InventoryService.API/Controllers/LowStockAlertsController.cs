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
    public class LowStockAlertsController(ILowStockAlertService lowStockAlertService)
        : BaseController<ILowStockAlertService>(lowStockAlertService)
    {
        [HttpPost]
        public async Task<ActionResult<LowStockAlertServiceDTOs.GetAllLowStockAlertsResponseDTO>> GetAll(
            LowStockAlertServiceDTOs.GetAllLowStockAlertsRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetAllAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<LowStockAlertServiceDTOs.LowStockAlertDTO>> GetById(
            LowStockAlertServiceDTOs.GetLowStockAlertByIdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetByIdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<LowStockAlertServiceDTOs.ResolveLowStockAlertResponseDTO>> Resolve(
            LowStockAlertServiceDTOs.ResolveLowStockAlertRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.ResolveAsync(request, cancellationToken));
        }
    }
}