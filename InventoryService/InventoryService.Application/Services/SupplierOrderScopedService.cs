using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services.DTOs;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Domain.Enums;
using Shared.Contracts.Events;
using Shared.Kernel.Database;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Mapping;
using Shared.Kernel.Requests;
using Shared.Kernel.Services;
using static InventoryService.Domain.Exceptions.GeneralExceptions;

namespace InventoryService.Application.Services
{
    public class SupplierOrderScopedService : BaseService, ISupplierOrderService
    {
        private readonly ISupplierOrderRepository _supplierOrderRepository;
        private readonly ISupplierOrderItemRepository _supplierOrderItemRepository;
        private readonly IStockItemRepository _stockItemRepository;
        private readonly IStockMovementRepository _stockMovementRepository;
        private readonly IOutboxMessageService _outboxMessageService;

        public SupplierOrderScopedService(IUnitOfWork unitOfWork, IRequestContext requestContext,
            ISupplierOrderRepository supplierOrderRepository, ISupplierOrderItemRepository supplierOrderItemRepository,
            IStockItemRepository stockItemRepository, IStockMovementRepository stockMovementRepository,
            IOutboxMessageService outboxMessageService, IMapperWrapper mapper)
            : base(unitOfWork, requestContext, mapper)
        {
            _supplierOrderRepository = supplierOrderRepository;
            _supplierOrderItemRepository = supplierOrderItemRepository;
            _stockItemRepository = stockItemRepository;
            _stockMovementRepository = stockMovementRepository;
            _outboxMessageService = outboxMessageService;
        }

        public async Task<SupplierOrderServiceDTOs.SupplierOrderDTO> CreateAsync(
            SupplierOrderServiceDTOs.CreateSupplierOrderRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);
            RequestContext.EnsureWarehouseAccess(request.WarehouseId, UserRole.Admin);

            var order = SupplierOrder.Create(request.SupplierId, request.WarehouseId, RequestContext.UserId);
            _supplierOrderRepository.Add(order);

            var items = request.Items
                .Select(item => SupplierOrderItem.Create(order.Id, item.ProductId, item.Quantity, item.PurchasePrice))
                .ToList();

            foreach (var item in items)
            {
                _supplierOrderItemRepository.Add(item);
            }

            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return BuildSupplierOrderDTO(order, items);
        }

        public async Task<SupplierOrderServiceDTOs.GetAllSupplierOrdersResponseDTO> GetAllAsync(
            SupplierOrderServiceDTOs.GetAllSupplierOrdersRequestDTO request, CancellationToken cancellationToken)
        {
            var orders = await _supplierOrderRepository.GetAllAsync(cancellationToken);
            var dtos = new List<SupplierOrderServiceDTOs.SupplierOrderDTO>();

            foreach (var order in orders)
            {
                var items = await _supplierOrderItemRepository.GetBySupplierOrderIdAsync(order.Id, cancellationToken);
                dtos.Add(BuildSupplierOrderDTO(order, items));
            }

            return new SupplierOrderServiceDTOs.GetAllSupplierOrdersResponseDTO(dtos);
        }

        public async Task<SupplierOrderServiceDTOs.SupplierOrderDTO> GetByIdAsync(
            SupplierOrderServiceDTOs.GetSupplierOrderByIdRequestDTO request, CancellationToken cancellationToken)
        {
            var order = await _supplierOrderRepository.GetByIdAsync(request.SupplierOrderId, cancellationToken)
                ?? throw new NotFoundException(nameof(SupplierOrder), request.SupplierOrderId);

            var items = await _supplierOrderItemRepository.GetBySupplierOrderIdAsync(order.Id, cancellationToken);

            return BuildSupplierOrderDTO(order, items);
        }

        public async Task<SupplierOrderServiceDTOs.SubmitSupplierOrderResponseDTO> SubmitAsync(
            SupplierOrderServiceDTOs.SubmitSupplierOrderRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var order = await _supplierOrderRepository.GetByIdAsync(request.SupplierOrderId, cancellationToken)
                ?? throw new NotFoundException(nameof(SupplierOrder), request.SupplierOrderId);

            RequestContext.EnsureWarehouseAccess(order.WarehouseId, UserRole.Admin);

            order.Submit();
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new SupplierOrderServiceDTOs.SubmitSupplierOrderResponseDTO();
        }

        public async Task<SupplierOrderServiceDTOs.ReceiveSupplierOrderResponseDTO> ReceiveAsync(
            SupplierOrderServiceDTOs.ReceiveSupplierOrderRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Worker);

            var order = await _supplierOrderRepository.GetByIdAsync(request.SupplierOrderId, cancellationToken)
                ?? throw new NotFoundException(nameof(SupplierOrder), request.SupplierOrderId);

            RequestContext.EnsureWarehouseAccess(order.WarehouseId, UserRole.Manager);

            var orderItems = await _supplierOrderItemRepository.GetBySupplierOrderIdAsync(order.Id, cancellationToken);

            var requestedItemIds = request.Items.Select(item => item.SupplierOrderItemId).ToHashSet();

            if (orderItems.Count != request.Items.Count ||
                orderItems.Any(orderItem => !requestedItemIds.Contains(orderItem.Id)))
            {
                throw new SupplierOrderPartialReceiptNotSupportedException(order.Id);
            }

            var salePriceByItemId = request.Items.ToDictionary(item => item.SupplierOrderItemId, item => item.SalePrice);

            var operations = new List<IDirectOperation> { _supplierOrderRepository.BuildReceiveOperation(order.Id) };
            var createdStockItems = new List<StockItem>();

            foreach (var orderItem in orderItems)
            {
                var salePrice = salePriceByItemId[orderItem.Id];

                var stockItem = StockItem.Create(
                    orderItem.ProductId, order.WarehouseId, order.Id, orderItem.Quantity, salePrice);
                createdStockItems.Add(stockItem);

                var movement = StockMovement.Create(stockItem.Id, stockItem.ProductId, stockItem.WarehouseId,
                    stockItem.BatchId, stockItem.Price, StockMovementType.In, stockItem.Quantity, reference: null,
                    order.CreatedByUserId, RequestContext.UserId);

                operations.Add(_stockItemRepository.BuildCreateOperation(stockItem));
                operations.Add(_stockMovementRepository.BuildCreateOperation(movement));

                operations.Add(_outboxMessageService.BuildCreateOperation(
                    new StockQuantityChangedEvent(stockItem.ProductId, stockItem.WarehouseId, Increased: true)));
            }

            var success = await UnitOfWork.ExecuteInTransactionAsync(operations, cancellationToken);

            if (!success)
            {
                throw new SupplierOrderNotSubmittedException(order.Id);
            }

            return new SupplierOrderServiceDTOs.ReceiveSupplierOrderResponseDTO(
                Mapper.Map<List<StockServiceDTOs.StockItemDTO>>(createdStockItems));
        }

        private SupplierOrderServiceDTOs.SupplierOrderDTO BuildSupplierOrderDTO(
            SupplierOrder order, IReadOnlyList<SupplierOrderItem> items)
        {
            return new SupplierOrderServiceDTOs.SupplierOrderDTO(order.Id, order.SupplierId, order.WarehouseId,
                order.Status, order.CreatedByUserId, order.CreatedAt, order.SubmittedAt, order.ReceivedAt,
                Mapper.Map<List<SupplierOrderServiceDTOs.SupplierOrderItemDTO>>(items));
        }
    }
}