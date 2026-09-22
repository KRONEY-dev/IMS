using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using InventoryService.Domain.Exceptions;
using Moq;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Xunit;

namespace InventoryService.Tests.Application.Services
{
    public class LowStockAlertScopedServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IRequestContext> _requestContextMock = new();
        private readonly Mock<IMapperWrapper> _mapperMock = new();
        private readonly Mock<ILowStockAlertRepository> _lowStockAlertRepositoryMock = new();
        private readonly Mock<IStockThresholdRepository> _stockThresholdRepositoryMock = new();
        private readonly Mock<IStockItemRepository> _stockItemRepositoryMock = new();
        private readonly Mock<INotificationPublisher> _notificationPublisherMock = new();

        private LowStockAlertScopedService CreateSut()
        {
            return new LowStockAlertScopedService(
                _unitOfWorkMock.Object, _requestContextMock.Object,
                _lowStockAlertRepositoryMock.Object, _stockThresholdRepositoryMock.Object,
                _stockItemRepositoryMock.Object, _notificationPublisherMock.Object, _mapperMock.Object);
        }

        private void SetActor(UserRole role, IReadOnlyList<Guid>? warehouseIds = null)
        {
            _requestContextMock.SetupGet(context => context.Role).Returns(role.ToString());
            _requestContextMock.SetupGet(context => context.WarehouseIds).Returns(warehouseIds ?? []);
        }

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            var alertId = Guid.NewGuid();

            _lowStockAlertRepositoryMock
                .Setup(repo => repo.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LowStockAlert?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(
                new LowStockAlertServiceDTOs.GetLowStockAlertByIdRequestDTO(alertId), CancellationToken.None));
        }

        [Fact]
        public async Task ResolveAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActor(UserRole.Worker);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.ResolveAsync(
                new LowStockAlertServiceDTOs.ResolveLowStockAlertRequestDTO(Guid.NewGuid()), CancellationToken.None));
        }

        [Fact]
        public async Task ResolveAsync_NotFound_ThrowsNotFoundException()
        {
            var alertId = Guid.NewGuid();
            SetActor(UserRole.Manager, warehouseIds: []);

            _lowStockAlertRepositoryMock
                .Setup(repo => repo.GetByIdAsync(alertId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((LowStockAlert?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.ResolveAsync(
                new LowStockAlertServiceDTOs.ResolveLowStockAlertRequestDTO(alertId), CancellationToken.None));
        }

        [Fact]
        public async Task ResolveAsync_ManagerWithoutWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            var alert = LowStockAlert.Create(Guid.NewGuid(), Guid.NewGuid());
            SetActor(UserRole.Manager, warehouseIds: [Guid.NewGuid()]);

            _lowStockAlertRepositoryMock
                .Setup(repo => repo.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(alert);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.ResolveAsync(
                new LowStockAlertServiceDTOs.ResolveLowStockAlertRequestDTO(alert.Id), CancellationToken.None));
        }

        [Fact]
        public async Task ResolveAsync_Valid_ResolvesAlertAndSaves()
        {
            var alert = LowStockAlert.Create(Guid.NewGuid(), Guid.NewGuid());
            SetActor(UserRole.Admin);

            _lowStockAlertRepositoryMock
                .Setup(repo => repo.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(alert);

            var sut = CreateSut();

            await sut.ResolveAsync(new LowStockAlertServiceDTOs.ResolveLowStockAlertRequestDTO(alert.Id), CancellationToken.None);

            Assert.Equal(LowStockAlertStatus.Resolved, alert.Status);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ResolveAsync_AlreadyResolved_ThrowsLowStockAlertAlreadyResolvedException()
        {
            var alert = LowStockAlert.Create(Guid.NewGuid(), Guid.NewGuid());
            alert.Resolve();
            SetActor(UserRole.Admin);

            _lowStockAlertRepositoryMock
                .Setup(repo => repo.GetByIdAsync(alert.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(alert);

            var sut = CreateSut();

            await Assert.ThrowsAsync<GeneralExceptions.LowStockAlertAlreadyResolvedException>(() => sut.ResolveAsync(
                new LowStockAlertServiceDTOs.ResolveLowStockAlertRequestDTO(alert.Id), CancellationToken.None));
        }

        [Fact]
        public async Task EvaluateAfterDecreaseAsync_NoThresholdConfigured_NoOp()
        {
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockThreshold?)null);

            var sut = CreateSut();

            await sut.EvaluateAfterDecreaseAsync(productId, warehouseId, CancellationToken.None);

            _stockItemRepositoryMock.Verify(
                repo => repo.GetTotalQuantityAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
                Times.Never);

            _unitOfWorkMock.Verify(
                uow => uow.ExecuteInTransactionAsync(It.IsAny<IDirectOperation>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task EvaluateAfterDecreaseAsync_QuantityStillAtOrAboveReorderLevel_NoOp()
        {
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var threshold = StockThreshold.Create(productId, warehouseId, reorderLevel: 10, reorderQuantity: 5);

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(threshold);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetTotalQuantityAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(10);

            var sut = CreateSut();

            await sut.EvaluateAfterDecreaseAsync(productId, warehouseId, CancellationToken.None);

            _unitOfWorkMock.Verify(
                uow => uow.ExecuteInTransactionAsync(It.IsAny<IDirectOperation>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task EvaluateAfterDecreaseAsync_QuantityBelowReorderLevel_CreatesAlertViaTransaction()
        {
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var threshold = StockThreshold.Create(productId, warehouseId, reorderLevel: 10, reorderQuantity: 5);
            var createOperation = Mock.Of<IDirectOperation>();

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(threshold);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetTotalQuantityAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(9);

            _lowStockAlertRepositoryMock
                .Setup(repo => repo.BuildCreateOperation(It.Is<LowStockAlert>(
                    alert => alert.ProductId == productId && alert.WarehouseId == warehouseId)))
                .Returns(createOperation);

            var sut = CreateSut();

            await sut.EvaluateAfterDecreaseAsync(productId, warehouseId, CancellationToken.None);

            _lowStockAlertRepositoryMock.Verify(
                repo => repo.BuildCreateOperation(It.Is<LowStockAlert>(
                    alert => alert.ProductId == productId && alert.WarehouseId == warehouseId)),
                Times.Once);

            _unitOfWorkMock.Verify(
                uow => uow.ExecuteInTransactionAsync(createOperation, It.IsAny<CancellationToken>()),
                Times.Once);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyLowStockAlertAsync(
                    It.Is<NotificationServiceDTOs.LowStockAlertNotification>(n => n.ProductId == productId && n.WarehouseId == warehouseId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EvaluateAfterDecreaseAsync_UniqueConstraintViolationOnCreate_IsSwallowedNotPropagated()
        {
            // This only proves the service-layer catch swallows an exception thrown by a MOCKED
            // IUnitOfWork. It does NOT prove Postgres's real unique-index race guard is atomic
            // under actual concurrency — that requires a live database (Testcontainers, later phase).
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var threshold = StockThreshold.Create(productId, warehouseId, reorderLevel: 10, reorderQuantity: 5);

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(threshold);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetTotalQuantityAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(9);

            _lowStockAlertRepositoryMock
                .Setup(repo => repo.BuildCreateOperation(It.IsAny<LowStockAlert>()))
                .Returns(Mock.Of<IDirectOperation>());

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IDirectOperation>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new UniqueConstraintViolationException(new InvalidOperationException("simulated unique index violation")));

            var sut = CreateSut();

            var exception = await Record.ExceptionAsync(
                () => sut.EvaluateAfterDecreaseAsync(productId, warehouseId, CancellationToken.None));

            Assert.Null(exception);

            // Still notifies even though creation was swallowed as a duplicate — a concurrent
            // evaluation already created the active alert, so clients still need to hear about it.
            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyLowStockAlertAsync(
                    It.Is<NotificationServiceDTOs.LowStockAlertNotification>(n => n.ProductId == productId && n.WarehouseId == warehouseId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EvaluateAfterIncreaseAsync_NoThresholdConfigured_NoOp()
        {
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockThreshold?)null);

            var sut = CreateSut();

            await sut.EvaluateAfterIncreaseAsync(productId, warehouseId, CancellationToken.None);

            _unitOfWorkMock.Verify(
                uow => uow.ExecuteInTransactionAsync(It.IsAny<IDirectOperation>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task EvaluateAfterIncreaseAsync_QuantityStillBelowReorderLevel_NoOp()
        {
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var threshold = StockThreshold.Create(productId, warehouseId, reorderLevel: 10, reorderQuantity: 5);

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(threshold);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetTotalQuantityAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(9);

            var sut = CreateSut();

            await sut.EvaluateAfterIncreaseAsync(productId, warehouseId, CancellationToken.None);

            _unitOfWorkMock.Verify(
                uow => uow.ExecuteInTransactionAsync(It.IsAny<IDirectOperation>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task EvaluateAfterIncreaseAsync_QuantityAtOrAboveReorderLevel_ResolvesAlertViaTransaction()
        {
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var threshold = StockThreshold.Create(productId, warehouseId, reorderLevel: 10, reorderQuantity: 5);
            var resolveOperation = Mock.Of<IDirectOperation>();

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(threshold);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetTotalQuantityAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(10);

            _lowStockAlertRepositoryMock
                .Setup(repo => repo.BuildResolveOperation(productId, warehouseId))
                .Returns(resolveOperation);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(resolveOperation, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            await sut.EvaluateAfterIncreaseAsync(productId, warehouseId, CancellationToken.None);

            _lowStockAlertRepositoryMock.Verify(repo => repo.BuildResolveOperation(productId, warehouseId), Times.Once);

            _unitOfWorkMock.Verify(
                uow => uow.ExecuteInTransactionAsync(resolveOperation, It.IsAny<CancellationToken>()),
                Times.Once);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyLowStockAlertAsync(
                    It.Is<NotificationServiceDTOs.LowStockAlertNotification>(n => n.ProductId == productId && n.WarehouseId == warehouseId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task EvaluateAfterIncreaseAsync_ResolveOperationAffectsNoRows_DoesNotNotify()
        {
            // No active alert existed to resolve (already resolved by a concurrent evaluation) —
            // ExecuteInTransactionAsync reports 0 affected rows, so no notification should fire.
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var threshold = StockThreshold.Create(productId, warehouseId, reorderLevel: 10, reorderQuantity: 5);
            var resolveOperation = Mock.Of<IDirectOperation>();

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(threshold);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetTotalQuantityAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(10);

            _lowStockAlertRepositoryMock
                .Setup(repo => repo.BuildResolveOperation(productId, warehouseId))
                .Returns(resolveOperation);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(resolveOperation, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var sut = CreateSut();

            await sut.EvaluateAfterIncreaseAsync(productId, warehouseId, CancellationToken.None);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyLowStockAlertAsync(
                    It.IsAny<NotificationServiceDTOs.LowStockAlertNotification>(), It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
