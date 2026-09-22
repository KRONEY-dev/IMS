using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
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
    public class StockTransferScopedServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IRequestContext> _requestContextMock = new();
        private readonly Mock<IMapperWrapper> _mapperMock = new();
        private readonly Mock<IStockTransferRepository> _stockTransferRepositoryMock = new();
        private readonly Mock<IStockItemRepository> _stockItemRepositoryMock = new();
        private readonly Mock<IStockMovementRepository> _stockMovementRepositoryMock = new();
        private readonly Mock<IOutboxMessageService> _outboxMessageServiceMock = new();
        private readonly Mock<INotificationPublisher> _notificationPublisherMock = new();

        public StockTransferScopedServiceTests()
        {
            // Direct-operation builders are irrelevant to the branch under test in most cases —
            // stub them to return a benign no-op operation so ExecuteInTransactionAsync's mocked
            // return value (not the operation list contents) drives success/failure in those tests.
            _stockItemRepositoryMock.Setup(repo => repo.BuildDecrementOperation(It.IsAny<Guid>(), It.IsAny<int>())).Returns(Mock.Of<IDirectOperation>());
            _stockItemRepositoryMock.Setup(repo => repo.BuildIncrementOperation(It.IsAny<Guid>(), It.IsAny<int>())).Returns(Mock.Of<IDirectOperation>());
            _stockItemRepositoryMock.Setup(repo => repo.BuildCreateOperation(It.IsAny<StockItem>())).Returns(Mock.Of<IDirectOperation>());
            _stockTransferRepositoryMock.Setup(repo => repo.BuildCreateOperation(It.IsAny<StockTransfer>())).Returns(Mock.Of<IDirectOperation>());
            _stockTransferRepositoryMock.Setup(repo => repo.BuildReceiveOperation(It.IsAny<Guid>(), It.IsAny<int>(), It.IsAny<Guid>())).Returns(Mock.Of<IDirectOperation>());
            _stockTransferRepositoryMock.Setup(repo => repo.BuildCancelOperation(It.IsAny<Guid>(), It.IsAny<Guid>())).Returns(Mock.Of<IDirectOperation>());
            _stockMovementRepositoryMock.Setup(repo => repo.BuildCreateOperation(It.IsAny<StockMovement>())).Returns(Mock.Of<IDirectOperation>());
            _outboxMessageServiceMock.Setup(svc => svc.BuildCreateOperation(It.IsAny<StockQuantityChangedEvent>())).Returns(Mock.Of<IDirectOperation>());
        }

        private StockTransferScopedService CreateSut()
        {
            return new StockTransferScopedService(
                _unitOfWorkMock.Object, _requestContextMock.Object, _stockTransferRepositoryMock.Object,
                _stockItemRepositoryMock.Object, _stockMovementRepositoryMock.Object, _outboxMessageServiceMock.Object,
                _notificationPublisherMock.Object, _mapperMock.Object);
        }

        private void SetActor(UserRole role, Guid? userId = null, IReadOnlyList<Guid>? warehouseIds = null)
        {
            _requestContextMock.SetupGet(context => context.Role).Returns(role.ToString());
            _requestContextMock.SetupGet(context => context.UserId).Returns(userId ?? Guid.NewGuid());
            _requestContextMock.SetupGet(context => context.WarehouseIds).Returns(warehouseIds ?? []);
        }

        private static StockTransfer CreateTransfer(Guid sourceWarehouseId, Guid destinationWarehouseId,
            int quantity = 10, Guid? productId = null, Guid? batchId = null)
        {
            return StockTransfer.Create(Guid.NewGuid(), productId ?? Guid.NewGuid(), sourceWarehouseId,
                destinationWarehouseId, batchId ?? Guid.NewGuid(), quantity, 5m, Guid.NewGuid(), DateTime.UtcNow.AddDays(3));
        }

        // ---- InitiateAsync ----

        [Fact]
        public async Task InitiateAsync_SourceStockItemNotFound_ThrowsNotFoundException()
        {
            var stockItemId = Guid.NewGuid();
            SetActor(UserRole.Worker);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByIdAsync(stockItemId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockItem?)null);

            var sut = CreateSut();

            var request = new StockTransferServiceDTOs.InitiateTransferRequestDTO(
                Guid.NewGuid(), DateTime.UtcNow.AddDays(3),
                [new StockTransferServiceDTOs.StockTransferLineDTO(stockItemId, Guid.NewGuid(), 5)]);

            await Assert.ThrowsAsync<NotFoundException>(() => sut.InitiateAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task InitiateAsync_BatchMismatch_ThrowsStockItemBatchMismatchException()
        {
            var sourceItem = StockItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 20, 5m);
            SetActor(UserRole.Worker);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByIdAsync(sourceItem.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sourceItem);

            var sut = CreateSut();

            var request = new StockTransferServiceDTOs.InitiateTransferRequestDTO(
                Guid.NewGuid(), DateTime.UtcNow.AddDays(3),
                [new StockTransferServiceDTOs.StockTransferLineDTO(sourceItem.Id, Guid.NewGuid(), 5)]);

            await Assert.ThrowsAsync<StockItemBatchMismatchException>(() => sut.InitiateAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task InitiateAsync_WorkerWithoutSourceWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            var sourceItem = StockItem.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), 20, 5m);
            SetActor(UserRole.Worker, warehouseIds: [Guid.NewGuid()]);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByIdAsync(sourceItem.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sourceItem);

            var sut = CreateSut();

            var request = new StockTransferServiceDTOs.InitiateTransferRequestDTO(
                Guid.NewGuid(), DateTime.UtcNow.AddDays(3),
                [new StockTransferServiceDTOs.StockTransferLineDTO(sourceItem.Id, sourceItem.BatchId, 5)]);

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.InitiateAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task InitiateAsync_Valid_BuildsOperationsAndNotifiesPerLine()
        {
            var sourceWarehouseId = Guid.NewGuid();
            var destinationWarehouseId = Guid.NewGuid();
            var sourceItem = StockItem.Create(Guid.NewGuid(), sourceWarehouseId, Guid.NewGuid(), 20, 5m);
            SetActor(UserRole.Manager, warehouseIds: [sourceWarehouseId]);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByIdAsync(sourceItem.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sourceItem);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mapperMock
                .Setup(mapper => mapper.Map<List<StockTransferServiceDTOs.StockTransferDTO>>(It.IsAny<List<StockTransfer>>()))
                .Returns([]);

            var sut = CreateSut();

            var request = new StockTransferServiceDTOs.InitiateTransferRequestDTO(
                destinationWarehouseId, DateTime.UtcNow.AddDays(3),
                [new StockTransferServiceDTOs.StockTransferLineDTO(sourceItem.Id, sourceItem.BatchId, 5)]);

            var response = await sut.InitiateAsync(request, CancellationToken.None);

            Assert.NotEqual(Guid.Empty, response.ShipmentId);

            _stockItemRepositoryMock.Verify(repo => repo.BuildDecrementOperation(sourceItem.Id, 5), Times.Once);
            _stockTransferRepositoryMock.Verify(repo => repo.BuildCreateOperation(It.IsAny<StockTransfer>()), Times.Once);
            _stockMovementRepositoryMock.Verify(
                repo => repo.BuildCreateOperation(It.Is<StockMovement>(m => m.Type == StockMovementType.Out && m.Quantity == 5)),
                Times.Once);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(
                    It.Is<NotificationServiceDTOs.StockLevelChangedNotification>(n => n.ProductId == sourceItem.ProductId && n.WarehouseId == sourceWarehouseId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task InitiateAsync_TransactionFails_ThrowsShipmentInitiationFailedExceptionAndDoesNotNotify()
        {
            var sourceWarehouseId = Guid.NewGuid();
            var sourceItem = StockItem.Create(Guid.NewGuid(), sourceWarehouseId, Guid.NewGuid(), 20, 5m);
            SetActor(UserRole.Manager, warehouseIds: [sourceWarehouseId]);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByIdAsync(sourceItem.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sourceItem);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var sut = CreateSut();

            var request = new StockTransferServiceDTOs.InitiateTransferRequestDTO(
                Guid.NewGuid(), DateTime.UtcNow.AddDays(3),
                [new StockTransferServiceDTOs.StockTransferLineDTO(sourceItem.Id, sourceItem.BatchId, 5)]);

            await Assert.ThrowsAsync<ShipmentInitiationFailedException>(() => sut.InitiateAsync(request, CancellationToken.None));

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(It.IsAny<NotificationServiceDTOs.StockLevelChangedNotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ---- ReceiveAsync ----

        [Fact]
        public async Task ReceiveAsync_TransferNotFound_ThrowsNotFoundException()
        {
            var transferId = Guid.NewGuid();
            SetActor(UserRole.Worker);

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transferId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockTransfer?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.ReceiveAsync(
                new StockTransferServiceDTOs.ReceiveTransferRequestDTO(transferId, 5), CancellationToken.None));
        }

        [Fact]
        public async Task ReceiveAsync_WorkerWithoutDestinationWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            var transfer = CreateTransfer(Guid.NewGuid(), Guid.NewGuid());
            SetActor(UserRole.Worker, warehouseIds: [Guid.NewGuid()]);

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transfer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transfer);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.ReceiveAsync(
                new StockTransferServiceDTOs.ReceiveTransferRequestDTO(transfer.Id, 5), CancellationToken.None));
        }

        [Fact]
        public async Task ReceiveAsync_QuantityExceedsRemaining_ThrowsStockTransferQuantityExceedsRemainingException()
        {
            var destinationWarehouseId = Guid.NewGuid();
            var transfer = CreateTransfer(Guid.NewGuid(), destinationWarehouseId, quantity: 10);
            SetActor(UserRole.Manager, warehouseIds: [destinationWarehouseId]);

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transfer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transfer);

            var sut = CreateSut();

            await Assert.ThrowsAsync<StockTransferQuantityExceedsRemainingException>(() => sut.ReceiveAsync(
                new StockTransferServiceDTOs.ReceiveTransferRequestDTO(transfer.Id, 11), CancellationToken.None));
        }

        [Fact]
        public async Task ReceiveAsync_NoExistingDestinationBatch_CreatesNewStockItemViaTransaction()
        {
            var destinationWarehouseId = Guid.NewGuid();
            var transfer = CreateTransfer(Guid.NewGuid(), destinationWarehouseId, quantity: 10);
            SetActor(UserRole.Manager, warehouseIds: [destinationWarehouseId]);

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transfer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transfer);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByWarehouseAndBatchIdAsync(destinationWarehouseId, transfer.ProductId, transfer.BatchId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockItem?)null);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            var response = await sut.ReceiveAsync(
                new StockTransferServiceDTOs.ReceiveTransferRequestDTO(transfer.Id, 10), CancellationToken.None);

            Assert.Equal(0, response.RemainingQuantity);
            Assert.True(response.FullyReceived);

            _stockItemRepositoryMock.Verify(repo => repo.BuildCreateOperation(It.Is<StockItem>(
                item => item.ProductId == transfer.ProductId && item.WarehouseId == destinationWarehouseId
                    && item.BatchId == transfer.BatchId && item.Quantity == 10)), Times.Once);

            _stockItemRepositoryMock.Verify(repo => repo.BuildIncrementOperation(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(It.IsAny<NotificationServiceDTOs.StockLevelChangedNotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ReceiveAsync_ExistingDestinationBatch_IncrementsExistingStockItem()
        {
            var destinationWarehouseId = Guid.NewGuid();
            var transfer = CreateTransfer(Guid.NewGuid(), destinationWarehouseId, quantity: 10);
            var destinationItem = StockItem.Create(transfer.ProductId, destinationWarehouseId, transfer.BatchId, 3, transfer.Price);
            SetActor(UserRole.Manager, warehouseIds: [destinationWarehouseId]);

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transfer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transfer);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByWarehouseAndBatchIdAsync(destinationWarehouseId, transfer.ProductId, transfer.BatchId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(destinationItem);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            var response = await sut.ReceiveAsync(
                new StockTransferServiceDTOs.ReceiveTransferRequestDTO(transfer.Id, 4), CancellationToken.None);

            Assert.Equal(6, response.RemainingQuantity);
            Assert.False(response.FullyReceived);

            _stockItemRepositoryMock.Verify(repo => repo.BuildIncrementOperation(destinationItem.Id, 4), Times.Once);
            _stockItemRepositoryMock.Verify(repo => repo.BuildCreateOperation(It.IsAny<StockItem>()), Times.Never);
        }

        [Fact]
        public async Task ReceiveAsync_TransactionFails_ThrowsStockTransferNotInTransitExceptionAndDoesNotNotify()
        {
            var destinationWarehouseId = Guid.NewGuid();
            var transfer = CreateTransfer(Guid.NewGuid(), destinationWarehouseId, quantity: 10);
            SetActor(UserRole.Manager, warehouseIds: [destinationWarehouseId]);

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transfer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transfer);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByWarehouseAndBatchIdAsync(destinationWarehouseId, transfer.ProductId, transfer.BatchId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockItem?)null);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var sut = CreateSut();

            await Assert.ThrowsAsync<StockTransferNotInTransitException>(() => sut.ReceiveAsync(
                new StockTransferServiceDTOs.ReceiveTransferRequestDTO(transfer.Id, 10), CancellationToken.None));

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(It.IsAny<NotificationServiceDTOs.StockLevelChangedNotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }

        // ---- CancelAsync ----

        [Fact]
        public async Task CancelAsync_TransferNotFound_ThrowsNotFoundException()
        {
            var transferId = Guid.NewGuid();
            SetActor(UserRole.Worker);

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transferId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockTransfer?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.CancelAsync(
                new StockTransferServiceDTOs.CancelTransferRequestDTO(transferId), CancellationToken.None));
        }

        [Fact]
        public async Task CancelAsync_WorkerWithoutSourceWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            var transfer = CreateTransfer(Guid.NewGuid(), Guid.NewGuid());
            SetActor(UserRole.Worker, warehouseIds: [Guid.NewGuid()]);

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transfer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transfer);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.CancelAsync(
                new StockTransferServiceDTOs.CancelTransferRequestDTO(transfer.Id), CancellationToken.None));
        }

        [Fact]
        public async Task CancelAsync_SourceStockItemNotFound_ThrowsNotFoundException()
        {
            var sourceWarehouseId = Guid.NewGuid();
            var transfer = CreateTransfer(sourceWarehouseId, Guid.NewGuid());
            SetActor(UserRole.Manager, warehouseIds: [sourceWarehouseId]);

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transfer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transfer);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByWarehouseAndBatchIdAsync(sourceWarehouseId, transfer.ProductId, transfer.BatchId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockItem?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.CancelAsync(
                new StockTransferServiceDTOs.CancelTransferRequestDTO(transfer.Id), CancellationToken.None));
        }

        [Fact]
        public async Task CancelAsync_Valid_RestoresSourceQuantityAndNotifies()
        {
            var sourceWarehouseId = Guid.NewGuid();
            var transfer = CreateTransfer(sourceWarehouseId, Guid.NewGuid(), quantity: 7);
            var sourceItem = StockItem.Create(transfer.ProductId, sourceWarehouseId, transfer.BatchId, 3, transfer.Price);
            SetActor(UserRole.Manager, warehouseIds: [sourceWarehouseId]);

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transfer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transfer);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByWarehouseAndBatchIdAsync(sourceWarehouseId, transfer.ProductId, transfer.BatchId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sourceItem);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            var sut = CreateSut();

            await sut.CancelAsync(new StockTransferServiceDTOs.CancelTransferRequestDTO(transfer.Id), CancellationToken.None);

            _stockItemRepositoryMock.Verify(repo => repo.BuildIncrementOperation(sourceItem.Id, 7), Times.Once);
            _stockTransferRepositoryMock.Verify(repo => repo.BuildCancelOperation(transfer.Id, It.IsAny<Guid>()), Times.Once);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(
                    It.Is<NotificationServiceDTOs.StockLevelChangedNotification>(n => n.ProductId == transfer.ProductId && n.WarehouseId == sourceWarehouseId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task CancelAsync_TransactionFails_ThrowsStockTransferNotInTransitException()
        {
            var sourceWarehouseId = Guid.NewGuid();
            var transfer = CreateTransfer(sourceWarehouseId, Guid.NewGuid());
            var sourceItem = StockItem.Create(transfer.ProductId, sourceWarehouseId, transfer.BatchId, 3, transfer.Price);
            SetActor(UserRole.Manager, warehouseIds: [sourceWarehouseId]);

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transfer.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(transfer);

            _stockItemRepositoryMock
                .Setup(repo => repo.GetByWarehouseAndBatchIdAsync(sourceWarehouseId, transfer.ProductId, transfer.BatchId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(sourceItem);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var sut = CreateSut();

            await Assert.ThrowsAsync<StockTransferNotInTransitException>(() => sut.CancelAsync(
                new StockTransferServiceDTOs.CancelTransferRequestDTO(transfer.Id), CancellationToken.None));
        }

        // ---- Reads ----

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            var transferId = Guid.NewGuid();

            _stockTransferRepositoryMock
                .Setup(repo => repo.GetByIdAsync(transferId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((StockTransfer?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(
                new StockTransferServiceDTOs.GetStockTransferByIdRequestDTO(transferId), CancellationToken.None));
        }
    }
}
