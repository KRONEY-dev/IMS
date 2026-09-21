using InventoryService.Application.Options;
using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using Microsoft.Extensions.Options;
using Shared.Contracts.Events;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Shared.Kernel.Services;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Application.Services
{
    public class StockScopedService : BaseService, IStockService
    {
        private readonly IStockThresholdRepository _stockThresholdRepository;
        private readonly IStockItemRepository _stockItemRepository;
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly IOutboxMessageService _outboxMessageService;
        private readonly int _maxConcurrencyRetryAttempts;

        public StockScopedService(IUnitOfWork unitOfWork, IRequestContext requestContext,
            IStockThresholdRepository stockThresholdRepository, IStockItemRepository stockItemRepository,
            IStockMovementRepository stockMovementRepository, IOutboxMessageService outboxMessageService,
            IMapperWrapper mapper, IOptions<StockConcurrencySettings> stockConcurrencySettings)
            : base(unitOfWork, requestContext, mapper)
        {
            _stockThresholdRepository = stockThresholdRepository;
            _stockItemRepository = stockItemRepository;
            _stockMovementRepository = stockMovementRepository;
            _outboxMessageService = outboxMessageService;
            _maxConcurrencyRetryAttempts = stockConcurrencySettings.Value.MaxRetryAttempts;
        }

        public async Task<StockServiceDTOs.StockThresholdDTO> CreateThresholdAsync(
            StockServiceDTOs.CreateStockThresholdRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);
            RequestContext.EnsureWarehouseAccess(request.WarehouseId, UserRole.Admin);

            if (await _stockThresholdRepository.ExistsByProductAndWarehouseAsync(
                request.ProductId, request.WarehouseId, cancellationToken))
            {
                throw new StockThresholdAlreadyExistsException(request.ProductId, request.WarehouseId);
            }

            var threshold = StockThreshold.Create(
                request.ProductId, request.WarehouseId, request.ReorderLevel, request.ReorderQuantity);

            _stockThresholdRepository.Add(threshold);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return Mapper.Map<StockServiceDTOs.StockThresholdDTO>(threshold);
        }

        public async Task<StockServiceDTOs.GetAllStockThresholdsResponseDTO> GetAllThresholdsAsync(
            StockServiceDTOs.GetAllStockThresholdsRequestDTO request, CancellationToken cancellationToken)
        {
            var thresholds = await _stockThresholdRepository.GetAllAsync(cancellationToken);

            return new StockServiceDTOs.GetAllStockThresholdsResponseDTO(
                Mapper.Map<List<StockServiceDTOs.StockThresholdDTO>>(thresholds));
        }

        public async Task<StockServiceDTOs.StockThresholdDTO> GetThresholdByIdAsync(
            StockServiceDTOs.GetStockThresholdByIdRequestDTO request, CancellationToken cancellationToken)
        {
            var threshold = await _stockThresholdRepository.GetByIdAsync(request.StockThresholdId, cancellationToken)
                ?? throw new NotFoundException(nameof(StockThreshold), request.StockThresholdId);

            return Mapper.Map<StockServiceDTOs.StockThresholdDTO>(threshold);
        }

        public async Task<StockServiceDTOs.UpdateStockThresholdResponseDTO> UpdateThresholdAsync(
            StockServiceDTOs.UpdateStockThresholdRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var threshold = await _stockThresholdRepository.GetByIdAsync(request.StockThresholdId, cancellationToken)
                ?? throw new NotFoundException(nameof(StockThreshold), request.StockThresholdId);

            RequestContext.EnsureWarehouseAccess(threshold.WarehouseId, UserRole.Admin);

            threshold.UpdateThresholds(request.ReorderLevel, request.ReorderQuantity);

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    await UnitOfWork.SaveChangesAsync(cancellationToken);
                    return new StockServiceDTOs.UpdateStockThresholdResponseDTO();
                }
                catch (ConcurrencyConflictException) when (attempt < _maxConcurrencyRetryAttempts)
                {
                    await _stockThresholdRepository.ReloadAsync(threshold, cancellationToken);
                    threshold.UpdateThresholds(request.ReorderLevel, request.ReorderQuantity);
                }
            }
        }

        public async Task<StockServiceDTOs.DeleteStockThresholdResponseDTO> DeleteThresholdAsync(
            StockServiceDTOs.DeleteStockThresholdRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var threshold = await _stockThresholdRepository.GetByIdAsync(request.StockThresholdId, cancellationToken)
                ?? throw new NotFoundException(nameof(StockThreshold), request.StockThresholdId);

            RequestContext.EnsureWarehouseAccess(threshold.WarehouseId, UserRole.Admin);

            _stockThresholdRepository.Remove(threshold);
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new StockServiceDTOs.DeleteStockThresholdResponseDTO();
        }

        public async Task<StockServiceDTOs.StockItemDTO> ReceiveAsync(
            StockServiceDTOs.ReceiveStockRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Worker);
            RequestContext.EnsureWarehouseAccess(request.WarehouseId, UserRole.Manager);

            if (request.BatchId is { } batchId)
            {
                return await ReplenishExistingBatchAsync(request, batchId, cancellationToken);
            }

            var stockItem = StockItem.Create(
                request.ProductId, request.WarehouseId, Guid.NewGuid(), request.Quantity, request.Price);

            var movement = StockMovement.Create(stockItem.Id, stockItem.ProductId, stockItem.WarehouseId,
                stockItem.BatchId, stockItem.Price, StockMovementType.In, request.Quantity, reference: null,
                RequestContext.UserId, RequestContext.UserId);

            _stockItemRepository.Add(stockItem);
            _stockMovementRepository.Add(movement);
            _outboxMessageService.Add(new StockQuantityChangedEvent(stockItem.ProductId, stockItem.WarehouseId, Increased: true));
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return Mapper.Map<StockServiceDTOs.StockItemDTO>(stockItem);
        }

        private async Task<StockServiceDTOs.StockItemDTO> ReplenishExistingBatchAsync(
            StockServiceDTOs.ReceiveStockRequestDTO request, Guid batchId, CancellationToken cancellationToken)
        {
            var stockItem = await _stockItemRepository.GetByWarehouseAndBatchIdAsync(
                request.WarehouseId, request.ProductId, batchId, cancellationToken)
                ?? throw new NotFoundException(nameof(StockItem), batchId);

            if (stockItem.Price != request.Price)
            {
                throw new StockItemPriceMismatchException(stockItem.Id, stockItem.Price, request.Price);
            }

            var movement = StockMovement.Create(stockItem.Id, stockItem.ProductId, stockItem.WarehouseId,
                stockItem.BatchId, stockItem.Price, StockMovementType.In, request.Quantity, reference: null,
                RequestContext.UserId, RequestContext.UserId);

            var incrementOperation = _stockItemRepository.BuildIncrementOperation(stockItem.Id, request.Quantity);
            var insertMovementOperation = _stockMovementRepository.BuildCreateOperation(movement);

            var insertOutboxMessageOperation = _outboxMessageService.BuildCreateOperation(
                new StockQuantityChangedEvent(stockItem.ProductId, stockItem.WarehouseId, Increased: true));

            var success = await UnitOfWork.ExecuteInTransactionAsync(
                [incrementOperation, insertMovementOperation, insertOutboxMessageOperation], cancellationToken);

            if (!success)
            {
                throw new NotFoundException(nameof(StockItem), stockItem.Id);
            }

            await _stockItemRepository.ReloadAsync(stockItem, cancellationToken);

            return Mapper.Map<StockServiceDTOs.StockItemDTO>(stockItem);
        }

        public async Task<StockServiceDTOs.GetAllStockItemsResponseDTO> GetAllStockItemsAsync(
            StockServiceDTOs.GetAllStockItemsRequestDTO request, CancellationToken cancellationToken)
        {
            var stockItems = await _stockItemRepository.GetAllAsync(cancellationToken);

            return new StockServiceDTOs.GetAllStockItemsResponseDTO(
                Mapper.Map<List<StockServiceDTOs.StockItemDTO>>(stockItems));
        }

        public async Task<StockServiceDTOs.StockItemDTO> GetStockItemByIdAsync(
            StockServiceDTOs.GetStockItemByIdRequestDTO request, CancellationToken cancellationToken)
        {
            var stockItem = await _stockItemRepository.GetByIdAsync(request.StockItemId, cancellationToken)
                ?? throw new NotFoundException(nameof(StockItem), request.StockItemId);

            return Mapper.Map<StockServiceDTOs.StockItemDTO>(stockItem);
        }

        public async Task<StockServiceDTOs.SellStockResponseDTO> SellAsync(
            StockServiceDTOs.SellStockRequestDTO request, CancellationToken cancellationToken)
        {
            await DecrementStockAsync(request.StockItemId, request.Quantity, StockMovementType.Out, cancellationToken);

            return new StockServiceDTOs.SellStockResponseDTO();
        }

        public async Task<StockServiceDTOs.AdjustStockResponseDTO> AdjustAsync(
            StockServiceDTOs.AdjustStockRequestDTO request, CancellationToken cancellationToken)
        {
            await DecrementStockAsync(request.StockItemId, request.Quantity, StockMovementType.Adjustment, cancellationToken);

            return new StockServiceDTOs.AdjustStockResponseDTO();
        }

        public async Task<StockServiceDTOs.GetMovementsByStockItemIdResponseDTO> GetMovementsByStockItemIdAsync(
            StockServiceDTOs.GetMovementsByStockItemIdRequestDTO request, CancellationToken cancellationToken)
        {
            var movements = await _stockMovementRepository.GetByStockItemIdAsync(request.StockItemId, cancellationToken);

            return new StockServiceDTOs.GetMovementsByStockItemIdResponseDTO(
                Mapper.Map<List<StockServiceDTOs.StockMovementDTO>>(movements));
        }

        private async Task DecrementStockAsync(
            Guid stockItemId, int quantity, StockMovementType type, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Worker);

            var stockItem = await _stockItemRepository.GetByIdAsync(stockItemId, cancellationToken)
                ?? throw new NotFoundException(nameof(StockItem), stockItemId);

            RequestContext.EnsureWarehouseAccess(stockItem.WarehouseId, UserRole.Manager);

            var movement = StockMovement.Create(stockItem.Id, stockItem.ProductId, stockItem.WarehouseId,
                stockItem.BatchId, stockItem.Price, type, quantity, reference: null,
                RequestContext.UserId, RequestContext.UserId);

            var decrementOperation = _stockItemRepository.BuildDecrementOperation(stockItemId, quantity);
            var insertMovementOperation = _stockMovementRepository.BuildCreateOperation(movement);

            var insertOutboxMessageOperation = _outboxMessageService.BuildCreateOperation(
                new StockQuantityChangedEvent(stockItem.ProductId, stockItem.WarehouseId, Increased: false));

            var success = await UnitOfWork.ExecuteInTransactionAsync(
                [decrementOperation, insertMovementOperation, insertOutboxMessageOperation], cancellationToken);

            if (!success)
            {
                throw new InsufficientStockAvailableException(stockItemId, quantity);
            }
        }
    }
}