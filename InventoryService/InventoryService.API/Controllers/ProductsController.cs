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
    public class ProductsController(IProductService productService) : BaseController<IProductService>(productService)
    {
        [HttpPost]
        public async Task<ActionResult<ProductServiceDTOs.ProductDTO>> Create(
            ProductServiceDTOs.CreateProductRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.CreateAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<ProductServiceDTOs.GetAllProductsResponseDTO>> GetAll(
            ProductServiceDTOs.GetAllProductsRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetAllAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<ProductServiceDTOs.ProductDTO>> GetById(
            ProductServiceDTOs.GetProductByIdRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.GetByIdAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<ProductServiceDTOs.UpdateProductResponseDTO>> Update(
            ProductServiceDTOs.UpdateProductRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.UpdateAsync(request, cancellationToken));
        }

        [HttpPost]
        public async Task<ActionResult<ProductServiceDTOs.DeleteProductResponseDTO>> Delete(
            ProductServiceDTOs.DeleteProductRequestDTO request, CancellationToken cancellationToken)
        {
            return Ok(await MainService.DeleteAsync(request, cancellationToken));
        }
    }
}