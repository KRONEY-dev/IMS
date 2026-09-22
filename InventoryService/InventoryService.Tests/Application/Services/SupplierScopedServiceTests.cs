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
    public class SupplierScopedServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IRequestContext> _requestContextMock = new();
        private readonly Mock<IMapperWrapper> _mapperMock = new();
        private readonly Mock<ISupplierRepository> _supplierRepositoryMock = new();

        private SupplierScopedService CreateSut()
        {
            return new SupplierScopedService(_unitOfWorkMock.Object, _requestContextMock.Object,
                _supplierRepositoryMock.Object, _mapperMock.Object);
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
                new SupplierServiceDTOs.CreateSupplierRequestDTO("Acme", "acme@example.com", null), CancellationToken.None));

            _supplierRepositoryMock.Verify(repo => repo.Add(It.IsAny<Supplier>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ManagerOrAbove_AddsSupplierAndReturnsMappedDto()
        {
            SetActorRole(UserRole.Manager);

            var expectedDto = new SupplierServiceDTOs.SupplierDTO(Guid.NewGuid(), "Acme", "acme@example.com", null);
            _mapperMock
                .Setup(mapper => mapper.Map<SupplierServiceDTOs.SupplierDTO>(It.IsAny<Supplier>()))
                .Returns(expectedDto);

            var sut = CreateSut();

            var response = await sut.CreateAsync(
                new SupplierServiceDTOs.CreateSupplierRequestDTO("Acme", "acme@example.com", null), CancellationToken.None);

            Assert.Equal(expectedDto, response);

            _supplierRepositoryMock.Verify(
                repo => repo.Add(It.Is<Supplier>(s => s.Name == "Acme" && s.ContactEmail == "acme@example.com")),
                Times.Once);

            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsMappedSuppliers()
        {
            var suppliers = new List<Supplier> { Supplier.Create("Acme", null, null) };
            var expectedResponse = new SupplierServiceDTOs.GetAllSuppliersResponseDTO(
                [new SupplierServiceDTOs.SupplierDTO(suppliers[0].Id, "Acme", null, null)]);

            _supplierRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(suppliers);

            _mapperMock
                .Setup(mapper => mapper.Map<List<SupplierServiceDTOs.SupplierDTO>>(suppliers))
                .Returns(expectedResponse.Suppliers.ToList());

            var sut = CreateSut();

            var response = await sut.GetAllAsync(new SupplierServiceDTOs.GetAllSuppliersRequestDTO(), CancellationToken.None);

            Assert.Equal(expectedResponse.Suppliers, response.Suppliers);
        }

        [Fact]
        public async Task GetByIdAsync_SupplierNotFound_ThrowsNotFoundException()
        {
            var supplierId = Guid.NewGuid();

            _supplierRepositoryMock
                .Setup(repo => repo.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Supplier?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(
                () => sut.GetByIdAsync(new SupplierServiceDTOs.GetSupplierByIdRequestDTO(supplierId), CancellationToken.None));
        }

        [Fact]
        public async Task GetByIdAsync_SupplierFound_ReturnsMappedDto()
        {
            var supplier = Supplier.Create("Acme", "acme@example.com", "+10000000000");
            var expectedDto = new SupplierServiceDTOs.SupplierDTO(supplier.Id, "Acme", "acme@example.com", "+10000000000");

            _supplierRepositoryMock
                .Setup(repo => repo.GetByIdAsync(supplier.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(supplier);

            _mapperMock
                .Setup(mapper => mapper.Map<SupplierServiceDTOs.SupplierDTO>(supplier))
                .Returns(expectedDto);

            var sut = CreateSut();

            var response = await sut.GetByIdAsync(new SupplierServiceDTOs.GetSupplierByIdRequestDTO(supplier.Id), CancellationToken.None);

            Assert.Equal(expectedDto, response);
        }

        [Fact]
        public async Task UpdateAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActorRole(UserRole.Worker);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.UpdateAsync(
                new SupplierServiceDTOs.UpdateSupplierRequestDTO(Guid.NewGuid(), "New Name", null, null),
                CancellationToken.None));

            _supplierRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateAsync_SupplierNotFound_ThrowsNotFoundException()
        {
            SetActorRole(UserRole.Manager);

            var supplierId = Guid.NewGuid();

            _supplierRepositoryMock
                .Setup(repo => repo.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Supplier?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.UpdateAsync(
                new SupplierServiceDTOs.UpdateSupplierRequestDTO(supplierId, "New Name", null, null),
                CancellationToken.None));
        }

        [Fact]
        public async Task UpdateAsync_Valid_UpdatesSupplierFieldsAndSaves()
        {
            SetActorRole(UserRole.Manager);

            var supplier = Supplier.Create("Old Name", "old@example.com", "+10000000000");

            _supplierRepositoryMock
                .Setup(repo => repo.GetByIdAsync(supplier.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(supplier);

            var sut = CreateSut();

            await sut.UpdateAsync(
                new SupplierServiceDTOs.UpdateSupplierRequestDTO(supplier.Id, "New Name", "new@example.com", null),
                CancellationToken.None);

            Assert.Equal("New Name", supplier.Name);
            Assert.Equal("new@example.com", supplier.ContactEmail);
            Assert.Null(supplier.ContactPhone);

            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActorRole(UserRole.Worker);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.DeleteAsync(
                new SupplierServiceDTOs.DeleteSupplierRequestDTO(Guid.NewGuid()), CancellationToken.None));

            _supplierRepositoryMock.Verify(repo => repo.Remove(It.IsAny<Supplier>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_SupplierNotFound_ThrowsNotFoundException()
        {
            SetActorRole(UserRole.Manager);

            var supplierId = Guid.NewGuid();

            _supplierRepositoryMock
                .Setup(repo => repo.GetByIdAsync(supplierId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Supplier?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(
                () => sut.DeleteAsync(new SupplierServiceDTOs.DeleteSupplierRequestDTO(supplierId), CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_Valid_RemovesSupplierAndSaves()
        {
            SetActorRole(UserRole.Manager);

            var supplier = Supplier.Create("Acme", null, null);

            _supplierRepositoryMock
                .Setup(repo => repo.GetByIdAsync(supplier.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(supplier);

            var sut = CreateSut();

            await sut.DeleteAsync(new SupplierServiceDTOs.DeleteSupplierRequestDTO(supplier.Id), CancellationToken.None);

            _supplierRepositoryMock.Verify(repo => repo.Remove(supplier), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
