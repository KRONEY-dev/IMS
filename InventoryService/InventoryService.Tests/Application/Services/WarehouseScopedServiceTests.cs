using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Application.Services.DTOs;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Entities.ValueObjects;
using InventoryService.Domain.Enums;
using Moq;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Xunit;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Tests.Application.Services
{
    public class WarehouseScopedServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IRequestContext> _requestContextMock = new();
        private readonly Mock<IMapperWrapper> _mapperMock = new();
        private readonly Mock<IWarehouseRepository> _warehouseRepositoryMock = new();

        private WarehouseScopedService CreateSut()
        {
            return new WarehouseScopedService(_unitOfWorkMock.Object, _requestContextMock.Object,
                _warehouseRepositoryMock.Object, _mapperMock.Object);
        }

        private void SetActorRole(UserRole role, IReadOnlyList<Guid>? warehouseIds = null)
        {
            _requestContextMock.SetupGet(context => context.Role).Returns(role.ToString());
            _requestContextMock.SetupGet(context => context.WarehouseIds).Returns(warehouseIds ?? []);
        }

        private static List<WorkingHours> CreateFullWeek()
        {
            return Enum.GetValues<DayOfWeek>()
                .Select(day => WorkingHours.Create(day, isClosed: false, new TimeOnly(8, 0), new TimeOnly(20, 0)))
                .ToList();
        }

        private static Warehouse CreateWarehouse(WarehouseStatus status = WarehouseStatus.Active)
        {
            return new Warehouse(Guid.NewGuid(), "Test Warehouse", "Test Location", status, CreateFullWeek());
        }

        private static IReadOnlyList<WarehouseServiceDTOs.WorkingHoursDTO> CreateFullWeekDto()
        {
            return Enum.GetValues<DayOfWeek>()
                .Select(day => new WarehouseServiceDTOs.WorkingHoursDTO(day, false, new TimeOnly(8, 0), new TimeOnly(20, 0)))
                .ToList();
        }

        [Fact]
        public async Task CreateAsync_CallerBelowAdmin_ThrowsInsufficientPermissionsException()
        {
            SetActorRole(UserRole.Manager);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.CreateAsync(
                new WarehouseServiceDTOs.CreateWarehouseRequestDTO("Kyiv WH", "Kyiv", CreateFullWeekDto()),
                CancellationToken.None));

            _warehouseRepositoryMock.Verify(repo => repo.Add(It.IsAny<Warehouse>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_NameAlreadyTaken_ThrowsWarehouseNameAlreadyTakenException()
        {
            SetActorRole(UserRole.Admin);

            _warehouseRepositoryMock
                .Setup(repo => repo.ExistsByNameAsync("Kyiv WH", It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            await Assert.ThrowsAsync<WarehouseNameAlreadyTakenException>(() => sut.CreateAsync(
                new WarehouseServiceDTOs.CreateWarehouseRequestDTO("Kyiv WH", "Kyiv", CreateFullWeekDto()),
                CancellationToken.None));

            _warehouseRepositoryMock.Verify(repo => repo.Add(It.IsAny<Warehouse>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_Valid_AddsWarehouseAndReturnsMappedDto()
        {
            SetActorRole(UserRole.Admin);

            _warehouseRepositoryMock
                .Setup(repo => repo.ExistsByNameAsync("Kyiv WH", It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            _mapperMock
                .Setup(mapper => mapper.Map<List<WorkingHours>>(It.IsAny<object>()))
                .Returns(CreateFullWeek());

            var expectedDto = new WarehouseServiceDTOs.WarehouseDTO(
                Guid.NewGuid(), "Kyiv WH", "Kyiv", WarehouseStatus.Active, CreateFullWeekDto());
            _mapperMock
                .Setup(mapper => mapper.Map<WarehouseServiceDTOs.WarehouseDTO>(It.IsAny<Warehouse>()))
                .Returns(expectedDto);

            var sut = CreateSut();

            var response = await sut.CreateAsync(
                new WarehouseServiceDTOs.CreateWarehouseRequestDTO("Kyiv WH", "Kyiv", CreateFullWeekDto()),
                CancellationToken.None);

            Assert.Equal(expectedDto, response);

            _warehouseRepositoryMock.Verify(
                repo => repo.Add(It.Is<Warehouse>(w => w.Name == "Kyiv WH" && w.Location == "Kyiv")), Times.Once);

            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetByIdAsync_WarehouseNotFound_ThrowsNotFoundException()
        {
            var warehouseId = Guid.NewGuid();

            _warehouseRepositoryMock
                .Setup(repo => repo.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Warehouse?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(
                new WarehouseServiceDTOs.GetWarehouseByIdRequestDTO(warehouseId), CancellationToken.None));
        }

        [Fact]
        public async Task UpdateStatusAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActorRole(UserRole.Worker);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.UpdateStatusAsync(
                new WarehouseServiceDTOs.UpdateWarehouseStatusRequestDTO(Guid.NewGuid(), WarehouseStatus.Closed),
                CancellationToken.None));
        }

        [Fact]
        public async Task UpdateStatusAsync_ManagerWithoutWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            var warehouseId = Guid.NewGuid();
            SetActorRole(UserRole.Manager, warehouseIds: [Guid.NewGuid()]);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.UpdateStatusAsync(
                new WarehouseServiceDTOs.UpdateWarehouseStatusRequestDTO(warehouseId, WarehouseStatus.Closed),
                CancellationToken.None));

            _warehouseRepositoryMock.Verify(repo => repo.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task UpdateStatusAsync_ManagerWithWarehouseAccess_UpdatesStatusAndSaves()
        {
            var warehouse = CreateWarehouse();
            SetActorRole(UserRole.Manager, warehouseIds: [warehouse.Id]);

            _warehouseRepositoryMock
                .Setup(repo => repo.GetByIdAsync(warehouse.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(warehouse);

            var sut = CreateSut();

            await sut.UpdateStatusAsync(
                new WarehouseServiceDTOs.UpdateWarehouseStatusRequestDTO(warehouse.Id, WarehouseStatus.Closed),
                CancellationToken.None);

            Assert.Equal(WarehouseStatus.Closed, warehouse.Status);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateStatusAsync_AdminBypassesWarehouseMembership_UpdatesStatus()
        {
            var warehouse = CreateWarehouse();
            SetActorRole(UserRole.Admin, warehouseIds: []);

            _warehouseRepositoryMock
                .Setup(repo => repo.GetByIdAsync(warehouse.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(warehouse);

            var sut = CreateSut();

            await sut.UpdateStatusAsync(
                new WarehouseServiceDTOs.UpdateWarehouseStatusRequestDTO(warehouse.Id, WarehouseStatus.UnderMaintenance),
                CancellationToken.None);

            Assert.Equal(WarehouseStatus.UnderMaintenance, warehouse.Status);
        }

        [Fact]
        public async Task UpdateWorkingHoursAsync_WarehouseNotFound_ThrowsNotFoundException()
        {
            var warehouseId = Guid.NewGuid();
            SetActorRole(UserRole.Admin);

            _warehouseRepositoryMock
                .Setup(repo => repo.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Warehouse?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.UpdateWorkingHoursAsync(
                new WarehouseServiceDTOs.UpdateWarehouseWorkingHoursRequestDTO(warehouseId, CreateFullWeekDto()),
                CancellationToken.None));
        }

        [Fact]
        public async Task UpdateWorkingHoursAsync_Valid_UpdatesWorkingHoursAndSaves()
        {
            var warehouse = CreateWarehouse();
            SetActorRole(UserRole.Admin);

            var newWeek = Enum.GetValues<DayOfWeek>()
                .Select(day => WorkingHours.Create(day, isClosed: false, new TimeOnly(9, 0), new TimeOnly(18, 0)))
                .ToList();

            _warehouseRepositoryMock
                .Setup(repo => repo.GetByIdAsync(warehouse.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(warehouse);

            _mapperMock
                .Setup(mapper => mapper.Map<List<WorkingHours>>(It.IsAny<object>()))
                .Returns(newWeek);

            var sut = CreateSut();

            await sut.UpdateWorkingHoursAsync(
                new WarehouseServiceDTOs.UpdateWarehouseWorkingHoursRequestDTO(warehouse.Id, CreateFullWeekDto()),
                CancellationToken.None);

            Assert.Same(newWeek, warehouse.WorkingHours);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task DeleteAsync_CallerBelowAdmin_ThrowsInsufficientPermissionsException()
        {
            SetActorRole(UserRole.Manager);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.DeleteAsync(
                new WarehouseServiceDTOs.DeleteWarehouseRequestDTO(Guid.NewGuid()), CancellationToken.None));

            _warehouseRepositoryMock.Verify(repo => repo.Remove(It.IsAny<Warehouse>()), Times.Never);
        }

        [Fact]
        public async Task DeleteAsync_WarehouseNotFound_ThrowsNotFoundException()
        {
            SetActorRole(UserRole.Admin);

            var warehouseId = Guid.NewGuid();

            _warehouseRepositoryMock
                .Setup(repo => repo.GetByIdAsync(warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Warehouse?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.DeleteAsync(
                new WarehouseServiceDTOs.DeleteWarehouseRequestDTO(warehouseId), CancellationToken.None));
        }

        [Fact]
        public async Task DeleteAsync_Valid_RemovesWarehouseAndSaves()
        {
            SetActorRole(UserRole.Admin);

            var warehouse = CreateWarehouse();

            _warehouseRepositoryMock
                .Setup(repo => repo.GetByIdAsync(warehouse.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(warehouse);

            var sut = CreateSut();

            await sut.DeleteAsync(new WarehouseServiceDTOs.DeleteWarehouseRequestDTO(warehouse.Id), CancellationToken.None);

            _warehouseRepositoryMock.Verify(repo => repo.Remove(warehouse), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
