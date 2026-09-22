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
    public class SupplierOrderScopedServiceTests
    {
        private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
        private readonly Mock<IRequestContext> _requestContextMock = new();
        private readonly Mock<IMapperWrapper> _mapperMock = new();
        private readonly Mock<ISupplierOrderRepository> _supplierOrderRepositoryMock = new();
        private readonly Mock<ISupplierOrderItemRepository> _supplierOrderItemRepositoryMock = new();
        private readonly Mock<IStockItemRepository> _stockItemRepositoryMock = new();
        private readonly Mock<IStockMovementRepository> _stockMovementRepositoryMock = new();
        private readonly Mock<IOutboxMessageService> _outboxMessageServiceMock = new();
        private readonly Mock<INotificationPublisher> _notificationPublisherMock = new();

        public SupplierOrderScopedServiceTests()
        {
            _mapperMock
                .Setup(mapper => mapper.Map<List<SupplierOrderServiceDTOs.SupplierOrderItemDTO>>(It.IsAny<object>()))
                .Returns([]);

            _stockItemRepositoryMock.Setup(repo => repo.BuildCreateOperation(It.IsAny<StockItem>())).Returns(Mock.Of<IDirectOperation>());
            _stockMovementRepositoryMock.Setup(repo => repo.BuildCreateOperation(It.IsAny<StockMovement>())).Returns(Mock.Of<IDirectOperation>());
            _outboxMessageServiceMock.Setup(svc => svc.BuildCreateOperation(It.IsAny<StockQuantityChangedEvent>())).Returns(Mock.Of<IDirectOperation>());
            _supplierOrderRepositoryMock.Setup(repo => repo.BuildReceiveOperation(It.IsAny<Guid>())).Returns(Mock.Of<IDirectOperation>());
        }

        private SupplierOrderScopedService CreateSut()
        {
            return new SupplierOrderScopedService(
                _unitOfWorkMock.Object, _requestContextMock.Object, _supplierOrderRepositoryMock.Object,
                _supplierOrderItemRepositoryMock.Object, _stockItemRepositoryMock.Object, _stockMovementRepositoryMock.Object,
                _outboxMessageServiceMock.Object, _notificationPublisherMock.Object, _mapperMock.Object);
        }

        private void SetActor(UserRole role, Guid? userId = null, IReadOnlyList<Guid>? warehouseIds = null)
        {
            _requestContextMock.SetupGet(context => context.Role).Returns(role.ToString());
            _requestContextMock.SetupGet(context => context.UserId).Returns(userId ?? Guid.NewGuid());
            _requestContextMock.SetupGet(context => context.WarehouseIds).Returns(warehouseIds ?? []);
        }

        private static SupplierOrder CreateOrder(Guid warehouseId, Guid? createdByUserId = null)
        {
            return SupplierOrder.Create(Guid.NewGuid(), warehouseId, createdByUserId ?? Guid.NewGuid());
        }

        // ---- CreateAsync ----

        [Fact]
        public async Task CreateAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActor(UserRole.Worker);

            var sut = CreateSut();

            var request = new SupplierOrderServiceDTOs.CreateSupplierOrderRequestDTO(Guid.NewGuid(), Guid.NewGuid(), []);

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.CreateAsync(request, CancellationToken.None));

            _supplierOrderRepositoryMock.Verify(repo => repo.Add(It.IsAny<SupplierOrder>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_ManagerWithoutWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            SetActor(UserRole.Manager, warehouseIds: [Guid.NewGuid()]);

            var sut = CreateSut();

            var request = new SupplierOrderServiceDTOs.CreateSupplierOrderRequestDTO(Guid.NewGuid(), Guid.NewGuid(), []);

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.CreateAsync(request, CancellationToken.None));
        }

        [Fact]
        public async Task CreateAsync_Valid_AddsOrderAndItemsAndSaves()
        {
            var warehouseId = Guid.NewGuid();
            var supplierId = Guid.NewGuid();
            var productId = Guid.NewGuid();
            SetActor(UserRole.Admin);

            var sut = CreateSut();

            var request = new SupplierOrderServiceDTOs.CreateSupplierOrderRequestDTO(supplierId, warehouseId,
                [new SupplierOrderServiceDTOs.CreateSupplierOrderItemDTO(productId, 10, 3.5m)]);

            var response = await sut.CreateAsync(request, CancellationToken.None);

            Assert.Equal(supplierId, response.SupplierId);
            Assert.Equal(warehouseId, response.WarehouseId);
            Assert.Equal(SupplierOrderStatus.Created, response.Status);

            _supplierOrderRepositoryMock.Verify(repo => repo.Add(It.Is<SupplierOrder>(
                order => order.SupplierId == supplierId && order.WarehouseId == warehouseId)), Times.Once);

            _supplierOrderItemRepositoryMock.Verify(repo => repo.Add(It.Is<SupplierOrderItem>(
                item => item.ProductId == productId && item.Quantity == 10 && item.PurchasePrice == 3.5m)), Times.Once);

            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        // ---- GetByIdAsync ----

        [Fact]
        public async Task GetByIdAsync_NotFound_ThrowsNotFoundException()
        {
            var orderId = Guid.NewGuid();

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupplierOrder?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.GetByIdAsync(
                new SupplierOrderServiceDTOs.GetSupplierOrderByIdRequestDTO(orderId), CancellationToken.None));
        }

        [Fact]
        public async Task GetByIdAsync_Found_ReturnsDtoWithOrderFields()
        {
            var warehouseId = Guid.NewGuid();
            var order = CreateOrder(warehouseId);

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            _supplierOrderItemRepositoryMock
                .Setup(repo => repo.GetBySupplierOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([]);

            var sut = CreateSut();

            var response = await sut.GetByIdAsync(
                new SupplierOrderServiceDTOs.GetSupplierOrderByIdRequestDTO(order.Id), CancellationToken.None);

            Assert.Equal(order.Id, response.Id);
            Assert.Equal(warehouseId, response.WarehouseId);
            Assert.Equal(SupplierOrderStatus.Created, response.Status);
        }

        // ---- SubmitAsync ----

        [Fact]
        public async Task SubmitAsync_CallerBelowManager_ThrowsInsufficientPermissionsException()
        {
            SetActor(UserRole.Worker);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.SubmitAsync(
                new SupplierOrderServiceDTOs.SubmitSupplierOrderRequestDTO(Guid.NewGuid(), null), CancellationToken.None));
        }

        [Fact]
        public async Task SubmitAsync_NotFound_ThrowsNotFoundException()
        {
            var orderId = Guid.NewGuid();
            SetActor(UserRole.Manager, warehouseIds: []);

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupplierOrder?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.SubmitAsync(
                new SupplierOrderServiceDTOs.SubmitSupplierOrderRequestDTO(orderId, null), CancellationToken.None));
        }

        [Fact]
        public async Task SubmitAsync_ManagerWithoutWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            var order = CreateOrder(Guid.NewGuid());
            SetActor(UserRole.Manager, warehouseIds: [Guid.NewGuid()]);

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.SubmitAsync(
                new SupplierOrderServiceDTOs.SubmitSupplierOrderRequestDTO(order.Id, null), CancellationToken.None));
        }

        [Fact]
        public async Task SubmitAsync_Valid_SubmitsOrderAndSaves()
        {
            var order = CreateOrder(Guid.NewGuid());
            var expectedDelivery = DateTime.UtcNow.AddDays(5);
            SetActor(UserRole.Admin);

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var sut = CreateSut();

            await sut.SubmitAsync(
                new SupplierOrderServiceDTOs.SubmitSupplierOrderRequestDTO(order.Id, expectedDelivery), CancellationToken.None);

            Assert.Equal(SupplierOrderStatus.Submitted, order.Status);
            Assert.Equal(expectedDelivery, order.ExpectedDeliveryDate);
            _unitOfWorkMock.Verify(uow => uow.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SubmitAsync_AlreadySubmitted_ThrowsSupplierOrderNotCreatedException()
        {
            var order = CreateOrder(Guid.NewGuid());
            order.Submit(null);
            SetActor(UserRole.Admin);

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var sut = CreateSut();

            await Assert.ThrowsAsync<SupplierOrderNotCreatedException>(() => sut.SubmitAsync(
                new SupplierOrderServiceDTOs.SubmitSupplierOrderRequestDTO(order.Id, null), CancellationToken.None));
        }

        // ---- ReceiveAsync ----

        [Fact]
        public async Task ReceiveAsync_NotFound_ThrowsNotFoundException()
        {
            var orderId = Guid.NewGuid();
            SetActor(UserRole.Worker);

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(orderId, It.IsAny<CancellationToken>()))
                .ReturnsAsync((SupplierOrder?)null);

            var sut = CreateSut();

            await Assert.ThrowsAsync<NotFoundException>(() => sut.ReceiveAsync(
                new SupplierOrderServiceDTOs.ReceiveSupplierOrderRequestDTO(orderId, []), CancellationToken.None));
        }

        [Fact]
        public async Task ReceiveAsync_WorkerWithoutWarehouseAccess_ThrowsInsufficientPermissionsException()
        {
            var order = CreateOrder(Guid.NewGuid());
            SetActor(UserRole.Worker, warehouseIds: [Guid.NewGuid()]);

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            var sut = CreateSut();

            await Assert.ThrowsAsync<InsufficientPermissionsException>(() => sut.ReceiveAsync(
                new SupplierOrderServiceDTOs.ReceiveSupplierOrderRequestDTO(order.Id, []), CancellationToken.None));
        }

        [Fact]
        public async Task ReceiveAsync_ItemCountMismatch_ThrowsSupplierOrderPartialReceiptNotSupportedException()
        {
            var warehouseId = Guid.NewGuid();
            var order = CreateOrder(warehouseId);
            var orderItem = SupplierOrderItem.Create(order.Id, Guid.NewGuid(), 10, 3m);
            SetActor(UserRole.Worker, warehouseIds: [warehouseId]);

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            _supplierOrderItemRepositoryMock
                .Setup(repo => repo.GetBySupplierOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([orderItem]);

            var sut = CreateSut();

            // Fewer items supplied than exist on the order — partial receipt is not supported.
            await Assert.ThrowsAsync<SupplierOrderPartialReceiptNotSupportedException>(() => sut.ReceiveAsync(
                new SupplierOrderServiceDTOs.ReceiveSupplierOrderRequestDTO(order.Id, []), CancellationToken.None));
        }

        [Fact]
        public async Task ReceiveAsync_MismatchedItemIds_ThrowsSupplierOrderPartialReceiptNotSupportedException()
        {
            var warehouseId = Guid.NewGuid();
            var order = CreateOrder(warehouseId);
            var orderItem = SupplierOrderItem.Create(order.Id, Guid.NewGuid(), 10, 3m);
            SetActor(UserRole.Worker, warehouseIds: [warehouseId]);

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            _supplierOrderItemRepositoryMock
                .Setup(repo => repo.GetBySupplierOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([orderItem]);

            var sut = CreateSut();

            // Same count, but references an item ID that doesn't belong to this order.
            await Assert.ThrowsAsync<SupplierOrderPartialReceiptNotSupportedException>(() => sut.ReceiveAsync(
                new SupplierOrderServiceDTOs.ReceiveSupplierOrderRequestDTO(order.Id,
                    [new SupplierOrderServiceDTOs.ReceiveSupplierOrderItemDTO(Guid.NewGuid(), 5m)]), CancellationToken.None));
        }

        [Fact]
        public async Task ReceiveAsync_Valid_CreatesStockItemsAndNotifiesPerItemAndOrder()
        {
            var warehouseId = Guid.NewGuid();
            var order = CreateOrder(warehouseId);
            var orderItem = SupplierOrderItem.Create(order.Id, Guid.NewGuid(), 10, 3m);
            SetActor(UserRole.Worker, warehouseIds: [warehouseId]);

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            _supplierOrderItemRepositoryMock
                .Setup(repo => repo.GetBySupplierOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([orderItem]);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            _mapperMock
                .Setup(mapper => mapper.Map<List<StockServiceDTOs.StockItemDTO>>(It.IsAny<List<StockItem>>()))
                .Returns([]);

            var sut = CreateSut();

            await sut.ReceiveAsync(
                new SupplierOrderServiceDTOs.ReceiveSupplierOrderRequestDTO(order.Id,
                    [new SupplierOrderServiceDTOs.ReceiveSupplierOrderItemDTO(orderItem.Id, 7.5m)]), CancellationToken.None);

            _stockItemRepositoryMock.Verify(repo => repo.BuildCreateOperation(It.Is<StockItem>(
                item => item.ProductId == orderItem.ProductId && item.WarehouseId == warehouseId
                    && item.Quantity == orderItem.Quantity && item.Price == 7.5m)), Times.Once);

            _supplierOrderRepositoryMock.Verify(repo => repo.BuildReceiveOperation(order.Id), Times.Once);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(It.IsAny<NotificationServiceDTOs.StockLevelChangedNotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifySupplierOrderReceivedAsync(
                    It.Is<NotificationServiceDTOs.SupplierOrderReceivedNotification>(n => n.SupplierOrderId == order.Id && n.WarehouseId == warehouseId),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task ReceiveAsync_TransactionFails_ThrowsSupplierOrderNotSubmittedExceptionAndDoesNotNotify()
        {
            var warehouseId = Guid.NewGuid();
            var order = CreateOrder(warehouseId);
            var orderItem = SupplierOrderItem.Create(order.Id, Guid.NewGuid(), 10, 3m);
            SetActor(UserRole.Worker, warehouseIds: [warehouseId]);

            _supplierOrderRepositoryMock
                .Setup(repo => repo.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(order);

            _supplierOrderItemRepositoryMock
                .Setup(repo => repo.GetBySupplierOrderIdAsync(order.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync([orderItem]);

            _unitOfWorkMock
                .Setup(uow => uow.ExecuteInTransactionAsync(It.IsAny<IReadOnlyList<IDirectOperation>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var sut = CreateSut();

            await Assert.ThrowsAsync<SupplierOrderNotSubmittedException>(() => sut.ReceiveAsync(
                new SupplierOrderServiceDTOs.ReceiveSupplierOrderRequestDTO(order.Id,
                    [new SupplierOrderServiceDTOs.ReceiveSupplierOrderItemDTO(orderItem.Id, 7.5m)]), CancellationToken.None));

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifyStockLevelChangedAsync(It.IsAny<NotificationServiceDTOs.StockLevelChangedNotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);

            _notificationPublisherMock.Verify(
                publisher => publisher.NotifySupplierOrderReceivedAsync(It.IsAny<NotificationServiceDTOs.SupplierOrderReceivedNotification>(),
                    It.IsAny<CancellationToken>()),
                Times.Never);
        }
    }
}
