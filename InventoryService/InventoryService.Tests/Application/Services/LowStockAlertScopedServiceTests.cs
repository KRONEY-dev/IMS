using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Domain.Entities;
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

        private LowStockAlertScopedService CreateSut()
        {
            return new LowStockAlertScopedService(
                _unitOfWorkMock.Object, _requestContextMock.Object,
                _lowStockAlertRepositoryMock.Object, _stockThresholdRepositoryMock.Object,
                _stockItemRepositoryMock.Object, _mapperMock.Object);
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

            var sut = CreateSut();

            await sut.EvaluateAfterIncreaseAsync(productId, warehouseId, CancellationToken.None);

            _lowStockAlertRepositoryMock.Verify(repo => repo.BuildResolveOperation(productId, warehouseId), Times.Once);

            _unitOfWorkMock.Verify(
                uow => uow.ExecuteInTransactionAsync(resolveOperation, It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}
