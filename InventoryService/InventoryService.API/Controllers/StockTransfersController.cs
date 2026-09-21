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
    public class StockTransfersController(IStockTransferService stockTransferService)
        : BaseController<IStockTransferService>(stockTransferService)
    {
        [HttpPost]
        public async Task<ActionResult<StockTransferServiceDTOs.InitiateTransferResponseDTO>> Initiate(
            StockTransferServiceDTOs.InitiateTransferRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.InitiateAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockTransferServiceDTOs.GetShipmentByIdResponseDTO>> GetShipmentById(
            StockTransferServiceDTOs.GetShipmentByIdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetShipmentByIdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockTransferServiceDTOs.StockTransferDTO>> GetById(
            StockTransferServiceDTOs.GetStockTransferByIdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetByIdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockTransferServiceDTOs.ReceiveTransferResponseDTO>> Receive(
            StockTransferServiceDTOs.ReceiveTransferRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.ReceiveAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockTransferServiceDTOs.CancelTransferResponseDTO>> Cancel(
            StockTransferServiceDTOs.CancelTransferRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.CancelAsync(request, cancellationToken));
        }
    }
}