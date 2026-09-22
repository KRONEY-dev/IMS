using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Domain.Entities;
using Moq;
using Shared.Contracts.Events;
using Shared.Kernel.Database;
using Xunit;

namespace InventoryService.Tests.Application.Services
{
    public class OutboxMessageScopedServiceTests
    {
        private readonly Mock<IOutboxMessageRepository> _outboxMessageRepositoryMock = new();

        private OutboxMessageScopedService CreateSut()
        {
            return new OutboxMessageScopedService(_outboxMessageRepositoryMock.Object);
        }

        [Fact]
        public void Add_SerializesEventAndAddsUnderItsTypeName()
        {
            var eventData = new StockQuantityChangedEvent(Guid.NewGuid(), Guid.NewGuid(), Increased: true);

            var sut = CreateSut();

            sut.Add(eventData);

            _outboxMessageRepositoryMock.Verify(repo => repo.Add(It.Is<OutboxMessage>(
                message => message.EventType == nameof(StockQuantityChangedEvent)
                    && message.Payload.Contains(eventData.ProductId.ToString())
                    && message.ProcessedAt == null)),
                Times.Once);
        }

        [Fact]
        public void BuildCreateOperation_DelegatesToRepositoryWithSerializedEvent()
        {
            var eventData = new StockQuantityChangedEvent(Guid.NewGuid(), Guid.NewGuid(), Increased: false);
            var expectedOperation = Mock.Of<IDirectOperation>();

            _outboxMessageRepositoryMock
                .Setup(repo => repo.BuildCreateOperation(It.Is<OutboxMessage>(
                    message => message.EventType == nameof(StockQuantityChangedEvent))))
                .Returns(expectedOperation);

            var sut = CreateSut();

            var operation = sut.BuildCreateOperation(eventData);

            Assert.Same(expectedOperation, operation);
        }
    }
}
