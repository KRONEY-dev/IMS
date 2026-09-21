namespace InventoryService.Application.Services.DTOs
{
    public static class ProductServiceDTOs
    {
        public record CreateProductRequestDTO(string Name, string ProductCategory, string? PhotoUrl);

        public record GetAllProductsRequestDTO();
        public record GetAllProductsResponseDTO(IReadOnlyList<ProductDTO> Products);

        public record GetProductByIdRequestDTO(Guid ProductId);

        public record UpdateProductRequestDTO(Guid ProductId, string Name, string ProductCategory, string? PhotoUrl);
        public record UpdateProductResponseDTO();

        public record DeleteProductRequestDTO(Guid ProductId);
        public record DeleteProductResponseDTO();

        public record ProductDTO(Guid Id, string Name, string ProductCategory, string? PhotoUrl);
    }
}