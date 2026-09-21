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
    public class StockController(IStockService stockService) : BaseController<IStockService>(stockService)
    {
        [HttpPost]
        public async Task<ActionResult<StockServiceDTOs.StockThresholdDTO>> CreateThreshold(
            StockServiceDTOs.CreateStockThresholdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.CreateThresholdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockServiceDTOs.GetAllStockThresholdsResponseDTO>> GetAllThresholds(
            StockServiceDTOs.GetAllStockThresholdsRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetAllThresholdsAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockServiceDTOs.StockThresholdDTO>> GetThresholdById(
            StockServiceDTOs.GetStockThresholdByIdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetThresholdByIdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockServiceDTOs.UpdateStockThresholdResponseDTO>> UpdateThreshold(
            StockServiceDTOs.UpdateStockThresholdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.UpdateThresholdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockServiceDTOs.DeleteStockThresholdResponseDTO>> DeleteThreshold(
            StockServiceDTOs.DeleteStockThresholdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.DeleteThresholdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockServiceDTOs.StockItemDTO>> Receive(
            StockServiceDTOs.ReceiveStockRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.ReceiveAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockServiceDTOs.GetAllStockItemsResponseDTO>> GetAllStockItems(
            StockServiceDTOs.GetAllStockItemsRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetAllStockItemsAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockServiceDTOs.StockItemDTO>> GetStockItemById(
            StockServiceDTOs.GetStockItemByIdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetStockItemByIdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockServiceDTOs.SellStockResponseDTO>> Sell(
            StockServiceDTOs.SellStockRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.SellAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockServiceDTOs.AdjustStockResponseDTO>> Adjust(
            StockServiceDTOs.AdjustStockRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.AdjustAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<StockServiceDTOs.GetMovementsByStockItemIdResponseDTO>> GetMovementsByStockItemId(
            StockServiceDTOs.GetMovementsByStockItemIdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetMovementsByStockItemIdAsync(request, cancellationToken));
        }
    }
}