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
    public class SupplierOrdersController(ISupplierOrderService supplierOrderService)
        : BaseController<ISupplierOrderService>(supplierOrderService)
    {
        [HttpPost]
        public async Task<ActionResult<SupplierOrderServiceDTOs.SupplierOrderDTO>> Create(
            SupplierOrderServiceDTOs.CreateSupplierOrderRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.CreateAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<SupplierOrderServiceDTOs.GetAllSupplierOrdersResponseDTO>> GetAll(
            SupplierOrderServiceDTOs.GetAllSupplierOrdersRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetAllAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<SupplierOrderServiceDTOs.SupplierOrderDTO>> GetById(
            SupplierOrderServiceDTOs.GetSupplierOrderByIdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetByIdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<SupplierOrderServiceDTOs.SubmitSupplierOrderResponseDTO>> Submit(
            SupplierOrderServiceDTOs.SubmitSupplierOrderRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.SubmitAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<SupplierOrderServiceDTOs.ReceiveSupplierOrderResponseDTO>> Receive(
            SupplierOrderServiceDTOs.ReceiveSupplierOrderRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.ReceiveAsync(request, cancellationToken));
        }
    }
}