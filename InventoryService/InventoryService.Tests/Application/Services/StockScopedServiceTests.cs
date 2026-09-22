using InventoryService.Application.Options;
using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using Microsoft.Extensions.Options;
using Moq;
using Shared.Contracts.Events;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Xunit;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Tests.Application.Services
{
    public class StockScopedServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IRequestContext> _requestContextMock = new();
        private readonly Mock<IMapperWrapper> _mapperMock = new();
        private readonly Mock<IStockThresholdRepository> _stockThresholdRepositoryMock = new();
        private readonly Mock<IStockItemRepository> _stockItemRepositoryMock = new();
        private readonly Mock<IStockMovementRepository> _stockMovementRepositoryMock = new();
        private readonly Mock<IOutboxMessageService> _outboxMessageServiceMock = new();
        private readonly Mock<INotificationPublisher> _notificationPublisherMock = new();

        private StockScopedService CreateSut(int maxRetryAttempts = 3)
        {
            return new StockScopedService(
                _unitOfWorkMock.Object, _requestContextMock.Object, _stockThresholdRepositoryMock.Object,
                _stockItemRepositoryMock.Object, _stockMovementRepositoryMock.Object, _outboxMessageServiceMock.Object,
                _notificationPublisherMock.Object, _mapperMock.Object,
                Options.Create(new StockConcurrencySettings { MaxRetryAttempts = maxRetryAttempts }));
        }

        private void SetActor(UserRole role, Guid? userId = null, IReadOnlyList<Guid>? warehouseIds = null)
        {
            _requestContextMock.SetupGet(context => context.Role).Returns(role.ToString());
            _requestContextMock.SetupGet(context => context.UserId).Returns(userId ?? Guid.NewGuid());
            _requestContextMock.SetupGet(context => context.WarehouseIds).Returns(warehouseIds ?? []);
        }

        // ---- Thresholds ----

        [Fact]
        public async Task CreateThresholdAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActor(UserRole.Worker);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.CreateThresholdAsync(
                new StockServiceDTOs.CreateStockThresholdRequestDTO(Guid.NewGuid(), Guid.NewGuid(), 10, 5),
                CancellationToken.None));
        }

        [Fact]
        public async Task CreateThresholdAsync_ManagerWithoutWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            var warehouseId = Guid.NewGuid();
            SetActor(UserRole.Manager, warehouseIds: [Guid.NewGuid()]);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.CreateThresholdAsync(
                new StockServiceDTOs.CreateStockThresholdRequestDTO(Guid.NewGuid(), warehouseId, 10, 5),
                CancellationToken.None));
        }

        [Fact]
        public async Task CreateThresholdAsync_AlreadyExists_ThrowsStockThresholdAlreadyExistsException()
        {
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            SetActor(UserRole.Admin);

            _stockThresholdRepositoryMock
                .Setup(repo => repo.ExistsByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            await Assert.ThrowsAsync<StockThresholdAlreadyExistsException>(() => sut.CreateThresholdAsync(
                new StockServiceDTOs.CreateStockThresholdRequestDTO(productId, warehouseId, 10, 5), CancellationToken.None));

            _stockThresholdRepositoryMock.Verify(repo => repo.Add(It.IsAny<StockThreshold>()), Times.Never);
        }

        [Fact]
        public async Task CreateThresholdAsync_Valid_AddsThresholdAndReturnsMappedDto()
        {
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            SetActor(UserRole.Admin);

            _stockThresholdRepositoryMock
                .Setup(repo => repo.ExistsByProductAndWarehouseAsync(productId, warehouseId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var expectedDto = new StockServiceDTOs.StockThresholdDTO(Guid.NewGuid(), productId, warehouseId, 10, 5);
            _mapperMock
                .Setup(mapper => mapper.Map<StockServiceDTOs.StockThresholdDTO>(It.IsAny<StockThreshold>()))
                .Returns(expectedDto);

            var sut = CreateSut();

            var response = await sut.CreateThresholdAsync(
                new StockServiceDTOs.CreateStockThresholdRequestDTO(productId, warehouseId, 10, 5), CancellationToken.None);

            Assert.Equal(expectedDto, response);
            _stockThresholdRepositoryMock.Verify(repo => repo.Add(It.IsAny<StockThreshold>()), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetThresholdByIdAsync_NotFound_ThrowsNotFoundException()
        {
            var thresholdId = Guid.NewGuid();

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByIdAsync(thresholdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockThreshold?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.GetThresholdByIdAsync(
                new StockServiceDTOs.GetStockThresholdByIdRequestDTO(thresholdId), CancellationToken.None));
        }

        [Fact]
        public async Task UpdateThresholdAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActor(UserRole.Worker);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.UpdateThresholdAsync(
                new StockServiceDTOs.UpdateStockThresholdRequestDTO(Guid.NewGuid(), 10, 5), CancellationToken.None));
        }

        [Fact]
        public async Task UpdateThresholdAsync_NotFound_ThrowsNotFoundException()
        {
            SetActor(UserRole.Manager, warehouseIds: []);

            var thresholdId = Guid.NewGuid();

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByIdAsync(thresholdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockThreshold?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.UpdateThresholdAsync(
                new StockServiceDTOs.UpdateStockThresholdRequestDTO(thresholdId, 10, 5), CancellationToken.None));
        }

        [Fact]
        public async Task UpdateThresholdAsync_ManagerWithoutWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            var threshold = StockThreshold.Create(Guid.NewGuid(), Guid.NewGuid(), 10, 5);
            SetActor(UserRole.Manager, warehouseIds: [Guid.NewGuid()]);

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByIdAsync(threshold.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(threshold);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.UpdateThresholdAsync(
                new StockServiceDTOs.UpdateStockThresholdRequestDTO(threshold.Id, 20, 8), CancellationToken.None));
        }

        [Fact]
        public async Task UpdateThresholdAsync_Valid_UpdatesThresholdAndSaves()
        {
            var threshold = StockThreshold.Create(Guid.NewGuid(), Guid.NewGuid(), 10, 5);
            SetActor(UserRole.Admin);

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByIdAsync(threshold.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(threshold);

            var sut = CreateSut();

            await sut.UpdateThresholdAsync(
                new StockServiceDTOs.UpdateStockThresholdRequestDTO(threshold.Id, 20, 8), CancellationToken.None);

            Assert.Equal(20, threshold.ReorderLevel);
            Assert.Equal(8, threshold.ReorderQuantity);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateThresholdAsync_ConcurrencyConflictBelowRetryLimit_ReloadsAndRetriesUntilSuccess()
        {
            var threshold = StockThreshold.Create(Guid.NewGuid(), Guid.NewGuid(), 10, 5);
            SetActor(UserRole.Admin);

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByIdAsync(threshold.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(threshold);

            _stockThresholdRepositoryMock
                .Setup(repo => repo.ReloadAsync(threshold, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var callCount = 0;
            _unitOfWorkMock
                .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .Returns(() =>
                {
                    callCount++;
                    return callCount == 1
                        ? throw new ConcurrencyConflictException(new InvalidOperationException("simulated xmin mismatch"))
                        : Task.CompletedTask;
                });

            var sut = CreateSut(maxRetryAttempts: 3);

            await sut.UpdateThresholdAsync(
                new StockServiceDTOs.UpdateStockThresholdRequestDTO(threshold.Id, 20, 8), CancellationToken.None);

            Assert.Equal(2, callCount);
            _stockThresholdRepositoryMock.Verify(repo => repo.ReloadAsync(threshold, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task UpdateThresholdAsync_ConcurrencyConflictAtRetryLimit_PropagatesException()
        {
            var threshold = StockThreshold.Create(Guid.NewGuid(), Guid.NewGuid(), 10, 5);
            SetActor(UserRole.Admin);

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByIdAsync(threshold.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(threshold);

            _unitOfWorkMock
                .Setup(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new ConcurrencyConflictException(new InvalidOperationException("simulated xmin mismatch")));

            var sut = CreateSut(maxRetryAttempts: 1);

            await Assert.ThrowsAsync<ConcurrencyConflictException>(() => sut.UpdateThresholdAsync(
                new StockServiceDTOs.UpdateStockThresholdRequestDTO(threshold.Id, 20, 8), CancellationToken.None));

            _stockThresholdRepositoryMock.Verify(repo => repo.ReloadAsync(It.IsAny<StockThreshold>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task DeleteThresholdAsync_NotFound_ThrowsNotFoundException()
        {
            SetActor(UserRole.Manager, warehouseIds: []);

            var thresholdId = Guid.NewGuid();

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByIdAsync(thresholdId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockThreshold?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.DeleteThresholdAsync(
                new StockServiceDTOs.DeleteStockThresholdRequestDTO(thresholdId), CancellationToken.None));
        }

        [Fact]
        public async Task DeleteThresholdAsync_Valid_RemovesThresholdAndSaves()
        {
            var threshold = StockThreshold.Create(Guid.NewGuid(), Guid.NewGuid(), 10, 5);
            SetActor(UserRole.Admin);

            _stockThresholdRepositoryMock
                .Setup(repo => repo.GetByIdAsync(threshold.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(threshold);

            var sut = CreateSut();

            await sut.DeleteThresholdAsync(
                new StockServiceDTOs.DeleteStockThresholdRequestDTO(threshold.Id), CancellationToken.None);

            _stockThresholdRepositoryMock.Verify(repo => repo.Remove(threshold), Times.Once);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---- Receive ----

        [Fact]
        public async Task ReceiveAsync_WorkerWithoutWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            var warehouseId = Guid.NewGuid();
            SetActor(UserRole.Worker, warehouseIds: [Guid.NewGuid()]);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.ReceiveAsync(
                new StockServiceDTOs.ReceiveStockRequestDTO(Guid.NewGuid(), warehouseId, 10, 5m), CancellationToken.None));

            _stockItemRepositoryMock.Verify(repo => repo.Add(It.IsAny<StockItem>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveAsync_NewBatch_CreatesStockItemAndMovementQueuesOutboxAndNotifies()
        {
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            SetActor(UserRole.Manager, warehouseIds: [warehouseId]);

            var expectedDto = new StockServiceDTOs.StockItemDTO(Guid.NewGuid(), productId, warehouseId, Guid.NewGuid(), 10, 5m);
            _mapperMock
                .Setup(mapper => mapper.Map<StockServiceDTOs.StockItemDTO>(It.IsAny<StockItem>()))
                .Returns(expectedDto);

            var sut = CreateSut();

            var response = await sut.ReceiveAsync(
                new StockServiceDTOs.ReceiveStockRequestDTO(productId, warehouseId, 10, 5m), CancellationToken.None);

            Assert.Equal(expectedDto, response);

            _stockItemRepositoryMock.Verify(
                repo => repo.Add(It.Is<StockItem>(item => item.ProductId == productId && item.WarehouseId == warehouseId
                    && item.Quantity == 10 && item.Price == 5m)),
                Times.Once);

            _stockMovementRepositoryMock.Verify(
                repo => repo.Add(It.Is<StockMovement>(movement => movement.Type == StockMovementType.In && movement.Quantity == 10)),
                Times.Once);

            _outboxMessageServiceMock.Verify(
                svc => svc.Add(It.Is<StockQuantityChangedEvent>(e => e.ProductId == productId && e.WarehouseId == warehouseId && e.Increased)),
                Times.Once);

            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(
                    It.Is<NotificationServiceDTOs.StockLevelChangedNotification>(n => n.ProductId == productId && n.WarehouseId == warehouseId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ReceiveAsync_ExistingBatchNotFound_ThrowsNotFoundException()
        {
            var productId = Guid.NewGuid();
            var warehouseId = Guid.NewGuid();
            var batchId = Guid.NewGuid();
            SetActor(UserRole.Manager, warehouseIds: [warehouseId]);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByWarehouseAndBatchIdAsync(warehouseId, productId, batchId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockItem?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.ReceiveAsync(
                new StockServiceDTOs.ReceiveStockRequestDTO(productId, warehouseId, 10, 5m, batchId), CancellationToken.None));
        }

        [Fact]
        public async Task ReceiveAsync_ExistingBatchPriceMismatch_ThrowsStockItemPriceMismatchException()
        {
            var warehouseId = Guid.NewGuid();
            var batchId = Guid.NewGuid();
            var stockItem = StockItem.Create(Guid.NewGuid(), warehouseId, batchId, 10, 5m);
            SetActor(UserRole.Manager, warehouseIds: [warehouseId]);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByWarehouseAndBatchIdAsync(warehouseId, stockItem.ProductId, batchId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stockItem);

            var sut = CreateSut();

            await Assert.ThrowsAsync<StockItemPriceMismatchException>(() => sut.ReceiveAsync(
                new StockServiceDTOs.ReceiveStockRequestDTO(stockItem.ProductId, warehouseId, 10, 7m, batchId), CancellationToken.None));
        }

        [Fact]
        public async Task ReceiveAsync_ExistingBatchValid_IncrementsViaTransactionReloadsAndNotifies()
        {
            var warehouseId = Guid.NewGuid();
            var batchId = Guid.NewGuid();
            var stockItem = StockItem.Create(Guid.NewGuid(), warehouseId, batchId, 10, 5m);
            SetActor(UserRole.Manager, warehouseIds: [warehouseId]);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByWarehouseAndBatchIdAsync(warehouseId, stockItem.ProductId, batchId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stockItem);

            _stockItemRepositoryMock
                .Setup(repo => repo.BuildIncrementOperation(stockItem.Id, 4))
                .Returns(Mock.Of<IDirectOperation>());

            _stockMovementRepositoryMock
                .Setup(repo => repo.BuildCreateOperation(It.IsAny<StockMovement>()))
                .Returns(Mock.Of<IDirectOperation>());

            _outboxMessageServiceMock
                .Setup(svc => svc.BuildCreateOperation(It.IsAny<StockQuantityChangedEvent>()))
                .Returns(Mock.Of<IDirectOperation>());

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _stockItemRepositoryMock
                .Setup(repo => repo.ReloadAsync(stockItem, It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var expectedDto = new StockServiceDTOs.StockItemDTO(stockItem.Id, stockItem.ProductId, warehouseId, batchId, 14, 5m);
            _mapperMock
                .Setup(mapper => mapper.Map<StockServiceDTOs.StockItemDTO>(stockItem))
                .Returns(expectedDto);

            var sut = CreateSut();

            var response = await sut.ReceiveAsync(
                new StockServiceDTOs.ReceiveStockRequestDTO(stockItem.ProductId, warehouseId, 4, 5m, batchId), CancellationToken.None);

            Assert.Equal(expectedDto, response);

            _stockItemRepositoryMock.Verify(repo => repo.ReloadAsync(stockItem, It.IsAny<CancellationToken>()), Times.Once);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(It.IsAny<NotificationServiceDTOs.StockLevelChangedNotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ReceiveAsync_ExistingBatchTransactionFails_ThrowsNotFoundException()
        {
            var warehouseId = Guid.NewGuid();
            var batchId = Guid.NewGuid();
            var stockItem = StockItem.Create(Guid.NewGuid(), warehouseId, batchId, 10, 5m);
            SetActor(UserRole.Manager, warehouseIds: [warehouseId]);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByWarehouseAndBatchIdAsync(warehouseId, stockItem.ProductId, batchId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stockItem);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.ReceiveAsync(
                new StockServiceDTOs.ReceiveStockRequestDTO(stockItem.ProductId, warehouseId, 4, 5m, batchId), CancellationToken.None));

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(It.IsAny<NotificationServiceDTOs.StockLevelChangedNotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ---- Sell / Adjust (both delegate to the same decrement path) ----

        [Fact]
        public async Task SellAsync_StockItemNotFound_ThrowsNotFoundException()
        {
            var stockItemId = Guid.NewGuid();
            SetActor(UserRole.Worker);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByIdAsync(stockItemId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockItem?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(
                () => sut.SellAsync(new StockServiceDTOs.SellStockRequestDTO(stockItemId, 5), CancellationToken.None));
        }

        [Fact]
        public async Task SellAsync_WorkerWithoutWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            var stockItem = StockItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 10, 5m);
            SetActor(UserRole.Worker, warehouseIds: [Guid.NewGuid()]);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByIdAsync(stockItem.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stockItem);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(
                () => sut.SellAsync(new StockServiceDTOs.SellStockRequestDTO(stockItem.Id, 5), CancellationToken.None));
        }

        [Fact]
        public async Task SellAsync_Valid_DecrementsViaTransactionAndNotifies()
        {
            var warehouseId = Guid.NewGuid();
            var stockItem = StockItem.Create(Guid.NewGuid(), warehouseId, Guid.NewGuid(), 10, 5m);
            SetActor(UserRole.Worker, warehouseIds: [warehouseId]);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByIdAsync(stockItem.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stockItem);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            await sut.SellAsync(new StockServiceDTOs.SellStockRequestDTO(stockItem.Id, 5), CancellationToken.None);

            _stockItemRepositoryMock.Verify(repo => repo.BuildDecrementOperation(stockItem.Id, 5), Times.Once);

            _stockMovementRepositoryMock.Verify(
                repo => repo.BuildCreateOperation(It.Is<StockMovement>(m => m.Type == StockMovementType.Out && m.Quantity == 5)),
                Times.Once);

            _outboxMessageServiceMock.Verify(
                svc => svc.BuildCreateOperation(It.Is<StockQuantityChangedEvent>(e => !e.Increased)), Times.Once);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(It.IsAny<NotificationServiceDTOs.StockLevelChangedNotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task SellAsync_InsufficientStock_ThrowsInsufficientStockAvailableException()
        {
            var warehouseId = Guid.NewGuid();
            var stockItem = StockItem.Create(Guid.NewGuid(), warehouseId, Guid.NewGuid(), 10, 5m);
            SetActor(UserRole.Worker, warehouseIds: [warehouseId]);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByIdAsync(stockItem.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stockItem);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientStockAvailableException>(
                () => sut.SellAsync(new StockServiceDTOs.SellStockRequestDTO(stockItem.Id, 999), CancellationToken.None));

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(It.IsAny<NotificationServiceDTOs.StockLevelChangedNotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        [Fact]
        public async Task AdjustAsync_Valid_RecordsAdjustmentTypeMovement()
        {
            var warehouseId = Guid.NewGuid();
            var stockItem = StockItem.Create(Guid.NewGuid(), warehouseId, Guid.NewGuid(), 10, 5m);
            SetActor(UserRole.Worker, warehouseIds: [warehouseId]);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByIdAsync(stockItem.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(stockItem);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            await sut.AdjustAsync(new StockServiceDTOs.AdjustStockRequestDTO(stockItem.Id, 2), CancellationToken.None);

            _stockMovementRepositoryMock.Verify(
                repo => repo.BuildCreateOperation(It.Is<StockMovement>(m => m.Type == StockMovementType.Adjustment && m.Quantity == 2)),
                Times.Once);
        }

        // ---- Reads ----

        [Fact]
        public async Task GetStockItemByIdAsync_NotFound_ThrowsNotFoundException()
        {
            var stockItemId = Guid.NewGuid();

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByIdAsync(stockItemId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockItem?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.GetStockItemByIdAsync(
                new StockServiceDTOs.GetStockItemByIdRequestDTO(stockItemId), CancellationToken.None));
        }

        [Fact]
        public async Task GetMovementsByStockItemIdAsync_ReturnsMappedMovements()
        {
            var stockItemId = Guid.NewGuid();
            var movements = new List<StockMovement>
            {
                StockMovement.Create(stockItemId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 5m,
                    StockMovementType.In, 10, null, Guid.NewGuid(), Guid.NewGuid())
            };

            var expectedDtos = new List<StockServiceDTOs.StockMovementDTO>
            {
                new(movements[0].Id, stockItemId, movements[0].ProductId, movements[0].WarehouseId, movements[0].BatchId,
                    5m, StockMovementType.In, 10, null, movements[0].InitiatedByUserId, movements[0].PerformedByUserId, movements[0].CreatedAt)
            };

            _stockMovementRepositoryMock
                .Setup(repo => repo.GetByStockItemIdAsync(stockItemId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(movements);

            _mapperMock
                .Setup(mapper => mapper.Map<List<StockServiceDTOs.StockMovementDTO>>(movements))
                .Returns(expectedDtos);

            var sut = CreateSut();

            var response = await sut.GetMovementsByStockItemIdAsync(
                new StockServiceDTOs.GetMovementsByStockItemIdRequestDTO(stockItemId), CancellationToken.None);

            Assert.Equal(expectedDtos, response.Movements);
        }
    }
}
