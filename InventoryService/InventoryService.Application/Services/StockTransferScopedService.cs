using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Shared.Kernel.Services;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Application.Services
{
    public class StockTransferScopedService : BaseService, IStockTransferService
    {
        private readonly IStockTransferRepository _stockTransferRepository;
        private readonly IStockItemRepository _stockItemRepository;
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly ILowStockAlertService _lowStockAlertService;

        public StockTransferScopedService(IUnitOfWork unitOfWork, IRequestContext requestContext,
            IStockTransferRepository stockTransferRepository, IStockItemRepository stockItemRepository,
            IStockMovementRepository stockMovementRepository, ILowStockAlertService lowStockAlertService,
            IMapperWrapper mapper) : base(unitOfWork, requestContext, mapper)
        {
            _stockTransferRepository = stockTransferRepository;
            _stockItemRepository = stockItemRepository;
            _stockMovementRepository = stockMovementRepository;
            _lowStockAlertService = lowStockAlertService;
        }

        public async Task<StockTransferServiceDTOs.InitiateTransferResponseDTO> InitiateAsync(
            StockTransferServiceDTOs.InitiateTransferRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Worker);

            var shipmentId = Guid.NewGuid();
            var operations = new List<IDirectOperation>();
            var transfers = new List<StockTransfer>();

            foreach (var line in request.Items)
            {
                var sourceItem = await _stockItemRepository.GetByIdAsync(line.StockItemId, cancellationToken)
                    ?? throw new NotFoundException(nameof(StockItem), line.StockItemId);

                if (sourceItem.BatchId != line.BatchId)
                {
                    throw new StockItemBatchMismatchException(sourceItem.Id, sourceItem.BatchId, line.BatchId);
                }

                RequestContext.EnsureWarehouseAccess(sourceItem.WarehouseId, UserRole.Manager);

                var transfer = StockTransfer.Create(shipmentId, sourceItem.ProductId, sourceItem.WarehouseId,
                    request.DestinationWarehouseId, sourceItem.BatchId, line.Quantity, sourceItem.Price,
                    RequestContext.UserId, request.ExpectedReceiptDate);

                transfers.Add(transfer);

                operations.Add(_stockItemRepository.BuildDecrementOperation(sourceItem.Id, line.Quantity));
                operations.Add(_stockTransferRepository.BuildCreateOperation(transfer));

                var movement = StockMovement.Create(sourceItem.Id, sourceItem.ProductId, sourceItem.WarehouseId,
                    sourceItem.BatchId, sourceItem.Price, StockMovementType.Out, line.Quantity, transfer.Id,
                    RequestContext.UserId, RequestContext.UserId);

                operations.Add(_stockMovementRepository.BuildCreateOperation(movement));
            }

            var success = await UnitOfWork.ExecuteInTransactionAsync(operations, cancellationToken);

            if (!success)
            {
                throw new ShipmentInitiationFailedException(shipmentId);
            }

            foreach (var transfer in transfers)
            {
                await _lowStockAlertService.EvaluateAfterDecreaseAsync(
                    transfer.ProductId, transfer.SourceWarehouseId, cancellationToken);
            }

            return new StockTransferServiceDTOs.InitiateTransferResponseDTO(
                shipmentId, Mapper.Map<List<StockTransferServiceDTOs.StockTransferDTO>>(transfers));
        }

        public async Task<StockTransferServiceDTOs.GetShipmentByIdResponseDTO> GetShipmentByIdAsync(
            StockTransferServiceDTOs.GetShipmentByIdRequestDTO request, CancellationToken cancellationToken)
        {
            var transfers = await _stockTransferRepository.GetByShipmentIdAsync(request.ShipmentId, cancellationToken);

            return new StockTransferServiceDTOs.GetShipmentByIdResponseDTO(
                Mapper.Map<List<StockTransferServiceDTOs.StockTransferDTO>>(transfers));
        }

        public async Task<StockTransferServiceDTOs.StockTransferDTO> GetByIdAsync(
            StockTransferServiceDTOs.GetStockTransferByIdRequestDTO request, CancellationToken cancellationToken)
        {
            var transfer = await _stockTransferRepository.GetByIdAsync(request.StockTransferId, cancellationToken)
                ?? throw new NotFoundException(nameof(StockTransfer), request.StockTransferId);

            return Mapper.Map<StockTransferServiceDTOs.StockTransferDTO>(transfer);
        }

        public async Task<StockTransferServiceDTOs.CompleteTransferResponseDTO> CompleteAsync(
            StockTransferServiceDTOs.CompleteTransferRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Worker);

            var transfer = await _stockTransferRepository.GetByIdAsync(request.StockTransferId, cancellationToken)
                ?? throw new NotFoundException(nameof(StockTransfer), request.StockTransferId);

            RequestContext.EnsureWarehouseAccess(transfer.DestinationWarehouseId, UserRole.Manager);

            var performedByUserId = RequestContext.UserId;

            var destinationItem = await _stockItemRepository.GetByWarehouseAndBatchIdAsync(
                transfer.DestinationWarehouseId, transfer.ProductId, transfer.BatchId, cancellationToken);

            var operations = new List<IDirectOperation>
            {
                _stockTransferRepository.BuildCompleteOperation(transfer.Id, performedByUserId)
            };

            Guid destinationStockItemId;

            if (destinationItem is not null)
            {
                destinationStockItemId = destinationItem.Id;
                operations.Add(_stockItemRepository.BuildIncrementOperation(destinationItem.Id, transfer.Quantity));
            }
            else
            {
                var newStockItem = StockItem.Create(transfer.ProductId, transfer.DestinationWarehouseId,
                    transfer.BatchId, transfer.Quantity, transfer.Price);
                destinationStockItemId = newStockItem.Id;
                operations.Add(_stockItemRepository.BuildCreateOperation(newStockItem));
            }

            var movement = StockMovement.Create(destinationStockItemId, transfer.ProductId,
                transfer.DestinationWarehouseId, transfer.BatchId, transfer.Price, StockMovementType.In,
                transfer.Quantity, transfer.Id, transfer.InitiatedByUserId, performedByUserId);

            operations.Add(_stockMovementRepository.BuildCreateOperation(movement));

            var success = await UnitOfWork.ExecuteInTransactionAsync(operations, cancellationToken);

            if (!success)
            {
                throw new StockTransferNotInTransitException(transfer.Id);
            }

            await _lowStockAlertService.EvaluateAfterIncreaseAsync(
                transfer.ProductId, transfer.DestinationWarehouseId, cancellationToken);

            return new StockTransferServiceDTOs.CompleteTransferResponseDTO();
        }

        public async Task<StockTransferServiceDTOs.CancelTransferResponseDTO> CancelAsync(
            StockTransferServiceDTOs.CancelTransferRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Worker);

            var transfer = await _stockTransferRepository.GetByIdAsync(request.StockTransferId, cancellationToken)
                ?? throw new NotFoundException(nameof(StockTransfer), request.StockTransferId);

            RequestContext.EnsureWarehouseAccess(transfer.SourceWarehouseId, UserRole.Manager);

            var performedByUserId = RequestContext.UserId;

            var sourceItem = await _stockItemRepository.GetByWarehouseAndBatchIdAsync(
                transfer.SourceWarehouseId, transfer.ProductId, transfer.BatchId, cancellationToken)
                ?? throw new NotFoundException(nameof(StockItem), transfer.BatchId);

            var movement = StockMovement.Create(sourceItem.Id, transfer.ProductId, transfer.SourceWarehouseId,
                transfer.BatchId, transfer.Price, StockMovementType.In, transfer.Quantity, transfer.Id,
                transfer.InitiatedByUserId, performedByUserId);

            var operations = new List<IDirectOperation>
            {
                _stockTransferRepository.BuildCancelOperation(transfer.Id, performedByUserId),
                _stockItemRepository.BuildIncrementOperation(sourceItem.Id, transfer.Quantity),
                _stockMovementRepository.BuildCreateOperation(movement)
            };

            var success = await UnitOfWork.ExecuteInTransactionAsync(operations, cancellationToken);

            if (!success)
            {
                throw new StockTransferNotInTransitException(transfer.Id);
            }

            await _lowStockAlertService.EvaluateAfterIncreaseAsync(
                transfer.ProductId, transfer.SourceWarehouseId, cancellationToken);

            return new StockTransferServiceDTOs.CancelTransferResponseDTO();
        }
    }
}