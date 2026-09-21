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
    public class ProductScopedService : BaseService, IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductScopedService(IUnitOfWork unitOfWork, IRequestContext requestContext,
            IProductRepository productRepository, IMapperWrapper mapper) : base(unitOfWork, requestContext, mapper)
        {
            _productRepository = productRepository;
        }

        public async Task<ProductServiceDTOs.ProductDTO> CreateAsync(
            ProductServiceDTOs.CreateProductRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var product = Product.Create(request.Name, request.ProductCategory, request.PhotoUrl);

            _productRepository.Add(product);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return Mapper.Map<ProductServiceDTOs.ProductDTO>(product);
        }

        public async Task<ProductServiceDTOs.GetAllProductsResponseDTO> GetAllAsync(
            ProductServiceDTOs.GetAllProductsRequestDTO request, CancellationToken cancellationToken)
        {
            var products = await _productRepository.GetAllAsync(cancellationToken);

            return new ProductServiceDTOs.GetAllProductsResponseDTO(
                Mapper.Map<List<ProductServiceDTOs.ProductDTO>>(products));
        }

        public async Task<ProductServiceDTOs.ProductDTO> GetByIdAsync(
            ProductServiceDTOs.GetProductByIdRequestDTO request, CancellationToken cancellationToken)
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
                ?? throw new NotFoundException(nameof(Product), request.ProductId);

            return Mapper.Map<ProductServiceDTOs.ProductDTO>(product);
        }

        public async Task<ProductServiceDTOs.UpdateProductResponseDTO> UpdateAsync(
            ProductServiceDTOs.UpdateProductRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
                ?? throw new NotFoundException(nameof(Product), request.ProductId);

            product.Rename(request.Name);
            product.ChangeCategory(request.ProductCategory);
            product.SetPhotoUrl(request.PhotoUrl);

            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new ProductServiceDTOs.UpdateProductResponseDTO();
        }

        public async Task<ProductServiceDTOs.DeleteProductResponseDTO> DeleteAsync(
            ProductServiceDTOs.DeleteProductRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken)
                ?? throw new NotFoundException(nameof(Product), request.ProductId);

            _productRepository.Remove(product);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new ProductServiceDTOs.DeleteProductResponseDTO();
        }
    }
}