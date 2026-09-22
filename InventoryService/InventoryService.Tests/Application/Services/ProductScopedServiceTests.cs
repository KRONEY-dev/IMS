using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Application.Services.DTOs;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using Moq;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Xunit;

namespace InventoryService.Tests.Application.Services
{
    public class ProductScopedServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IRequestContext> _requestContextMock = new();
        private readonly Mock<IMapperWrapper> _mapperMock = new();
        private readonly Mock<IProductRepository> _productRepositoryMock = new();

        private ProductScopedService CreateSut()
        {
            return new ProductScopedService(_unitOfWorkMock.Object, _requestContextMock.Object,
                _productRepositoryMock.Object, _mapperMock.Object);
        }

        private void SetActorRole(UserRole role)
        {
            _requestContextMock.SetupGet(context => context.Role).Returns(role.ToString());
        }

        [Fact]
        public async Task CreateAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActorRole(UserRole.Worker);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.CreateAsync(
                new ProductServiceDTOs.CreateProductRequestDTO("Widget", "Widgets", null), CancellationToken.None));

            _productRepositoryMock.Verify(repo => repo.Add(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ManagerOrAbove_AddsProductAndReturnsMappedDto()
        {
            SetActorRole(UserRole.Manager);

            var expectedDto = new ProductServiceDTOs.ProductDTO(Guid.NewGuid(), "Widget", "Widgets", null);
            _mapperMock
                .Setup(mapper => mapper.Map<ProductServiceDTOs.ProductDTO>(It.IsAny<Product>()))
                .Returns(expectedDto);

            var sut = CreateSut();

            var response = await sut.CreateAsync(
                new ProductServiceDTOs.CreateProductRequestDTO("Widget", "Widgets", null), CancellationToken.None);

            Assert.Equal(expectedDto, response);

            _productRepositoryMock.Verify(
                repo => repo.Add(It.Is<Product>(p => p.Name == "Widget" && p.ProductCategory == "Widgets")),
                Times.Once);

            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedProducts()
        {
            var products = new List<Product> { Product.Create("Widget", "Widgets", null) };
            var expectedResponse = new ProductServiceDTOs.GetAllProductsResponseDTO(
                [new ProductServiceDTOs.ProductDTO(products[0].Id, "Widget", "Widgets", null)]);

            _productRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(products);

            _mapperMock
                .Setup(mapper => mapper.Map<List<ProductServiceDTOs.ProductDTO>>(products))
                .Returns(expectedResponse.Products.ToList());

            var sut = CreateSut();

            var response = await sut.GetAllAsync(new ProductServiceDTOs.GetAllProductsRequestDTO(), CancellationToken.None);

            Assert.Equal(expectedResponse.Products, response.Products);
        }

        [Fact]
        public async Task GetByIdAsync_ProductNotFound_ThrowsNotFoundException()
        {
            var productId = Guid.NewGuid();

            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(
                () => sut.GetByIdAsync(new ProductServiceDTOs.GetProductByIdRequestDTO(productId), CancellationToken.None));
        }

        [Fact]
        public async Task GetByIdAsync_ProductFound_ReturnsMappedDto()
        {
            var product = Product.Create("Widget", "Widgets", null);
            var expectedDto = new ProductServiceDTOs.ProductDTO(product.Id, "Widget", "Widgets", null);

            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            _mapperMock
                .Setup(mapper => mapper.Map<ProductServiceDTOs.ProductDTO>(product))
                .Returns(expectedDto);

            var sut = CreateSut();

            var response = await sut.GetByIdAsync(new ProductServiceDTOs.GetProductByIdRequestDTO(product.Id), CancellationToken.None);

            Assert.Equal(expectedDto, response);
        }

        [Fact]
        public async Task UpdateAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActorRole(UserRole.Worker);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.UpdateAsync(
                new ProductServiceDTOs.UpdateProductRequestDTO(Guid.NewGuid(), "New Name", "New Category", null),
                CancellationToken.None));

            _productRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_ProductNotFound_ThrowsNotFoundException()
        {
            SetActorRole(UserRole.Manager);

            var productId = Guid.NewGuid();

            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.UpdateAsync(
                new ProductServiceDTOs.UpdateProductRequestDTO(productId, "New Name", "New Category", null),
                CancellationToken.None));
        }

        [Fact]
        public async Task UpdateAsync_Valid_UpdatesProductFieldsAndSaves()
        {
            SetActorRole(UserRole.Manager);

            var product = Product.Create("Old Name", "Old Category", "old-photo.png");

            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            var sut = CreateSut();

            await sut.UpdateAsync(
                new ProductServiceDTOs.UpdateProductRequestDTO(product.Id, "New Name", "New Category", "new-photo.png"),
                CancellationToken.None);

            Assert.Equal("New Name", product.Name);
            Assert.Equal("New Category", product.ProductCategory);
            Assert.Equal("new-photo.png", product.PhotoUrl);

            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActorRole(UserRole.Worker);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.DeleteAsync(
                new ProductServiceDTOs.DeleteProductRequestDTO(Guid.NewGuid()), CancellationToken.None));

            _productRepositoryMock.Verify(repo => repo.Remove(It.IsAny<Product>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_ProductNotFound_ThrowsNotFoundException()
        {
            SetActorRole(UserRole.Manager);

            var productId = Guid.NewGuid();

            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(productId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Product?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(
                () => sut.DeleteAsync(new ProductServiceDTOs.DeleteProductRequestDTO(productId), CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_Valid_RemovesProductAndSaves()
        {
            SetActorRole(UserRole.Manager);

            var product = Product.Create("Widget", "Widgets", null);

            _productRepositoryMock
                .Setup(repo => repo.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);

            var sut = CreateSut();

            await sut.DeleteAsync(new ProductServiceDTOs.DeleteProductRequestDTO(product.Id), CancellationToken.None);

            _productRepositoryMock.Verify(repo => repo.Remove(product), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
