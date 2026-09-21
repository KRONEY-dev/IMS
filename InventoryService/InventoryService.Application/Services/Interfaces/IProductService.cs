using InventoryService.Application.Services.DTOs;

namespace InventoryService.Application.Services.Interfaces
{
    public interface IProductService
    {
        Task<ProductServiceDTOs.ProductDTO> CreateAsync(
            ProductServiceDTOs.CreateProductRequestDTO request, CancellationToken cancellationToken);

        Task<ProductServiceDTOs.GetAllProductsResponseDTO> GetAllAsync(
            ProductServiceDTOs.GetAllProductsRequestDTO request, CancellationToken cancellationToken);

        Task<ProductServiceDTOs.ProductDTO> GetByIdAsync(
            ProductServiceDTOs.GetProductByIdRequestDTO request, CancellationToken cancellationToken);

        Task<ProductServiceDTOs.UpdateProductResponseDTO> UpdateAsync(
            ProductServiceDTOs.UpdateProductRequestDTO request, CancellationToken cancellationToken);

        Task<ProductServiceDTOs.DeleteProductResponseDTO> DeleteAsync(
            ProductServiceDTOs.DeleteProductRequestDTO request, CancellationToken cancellationToken);
    }
}