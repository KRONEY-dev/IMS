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

namespace InventoryService.Application.Services
{
    public class LowStockAlertScopedService : BaseService, ILowStockAlertService
    {
        private readonly ILowStockAlertRepository _lowStockAlertRepository;
        private readonly IStockThresholdRepository _stockThresholdRepository;
        private readonly IStockItemRepository _stockItemRepository;
        private readonly INotificationPublisher _notificationPublisher;

        public LowStockAlertScopedService(IUnitOfWork unitOfWork, IRequestContext requestContext,
            ILowStockAlertRepository lowStockAlertRepository, IStockThresholdRepository stockThresholdRepository,
            IStockItemRepository stockItemRepository, INotificationPublisher notificationPublisher,
            IMapperWrapper mapper)
            : base(unitOfWork, requestContext, mapper)
        {
            _lowStockAlertRepository = lowStockAlertRepository;
            _stockThresholdRepository = stockThresholdRepository;
            _stockItemRepository = stockItemRepository;
            _notificationPublisher = notificationPublisher;
        }

        public async Task<LowStockAlertServiceDTOs.GetAllLowStockAlertsResponseDTO> GetAllAsync(
            LowStockAlertServiceDTOs.GetAllLowStockAlertsRequestDTO request, CancellationToken cancellationToken)
        {
            var alerts = await _lowStockAlertRepository.GetAllAsync(cancellationToken);

            return new LowStockAlertServiceDTOs.GetAllLowStockAlertsResponseDTO(
                Mapper.Map<List<LowStockAlertServiceDTOs.LowStockAlertDTO>>(alerts));
        }

        public async Task<LowStockAlertServiceDTOs.LowStockAlertDTO> GetByIdAsync(
            LowStockAlertServiceDTOs.GetLowStockAlertByIdRequestDTO request, CancellationToken cancellationToken)
        {
            var alert = await _lowStockAlertRepository.GetByIdAsync(request.LowStockAlertId, cancellationToken)
                ?? throw new NotFoundException(nameof(LowStockAlert), request.LowStockAlertId);

            return Mapper.Map<LowStockAlertServiceDTOs.LowStockAlertDTO>(alert);
        }

        public async Task<LowStockAlertServiceDTOs.ResolveLowStockAlertResponseDTO> ResolveAsync(
            LowStockAlertServiceDTOs.ResolveLowStockAlertRequestDTO request, CancellationToken cancellationToken)
        {
            RequestContext.EnsureMinimumRole(UserRole.Manager);

            var alert = await _lowStockAlertRepository.GetByIdAsync(request.LowStockAlertId, cancellationToken)
                ?? throw new NotFoundException(nameof(LowStockAlert), request.LowStockAlertId);

            RequestContext.EnsureWarehouseAccess(alert.WarehouseId, UserRole.Admin);

            alert.Resolve();
            await UnitOfWork.SaveChangesAsync(cancellationToken);

            return new LowStockAlertServiceDTOs.ResolveLowStockAlertResponseDTO();
        }

        public async Task EvaluateAfterDecreaseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken)
        {
            var threshold = await _stockThresholdRepository.GetByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);

            if (threshold is null)
            {
                return;
            }

            var totalQuantity = await _stockItemRepository.GetTotalQuantityAsync(productId, warehouseId, cancellationToken);

            if (totalQuantity >= threshold.ReorderLevel)
            {
                return;
            }

            var alert = LowStockAlert.Create(productId, warehouseId);
            var createOperation = _lowStockAlertRepository.BuildCreateOperation(alert);

            try
            {
                await UnitOfWork.ExecuteInTransactionAsync(createOperation, cancellationToken);
            }
            catch (UniqueConstraintViolationException)
            {
                // An active alert already exists (concurrent evaluation created it first) — still worth notifying below.
            }

            await _notificationPublisher.NotifyLowStockAlertAsync(
                new NotificationServiceDTOs.LowStockAlertNotification(productId, warehouseId), cancellationToken);
        }

        public async Task EvaluateAfterIncreaseAsync(Guid productId, Guid warehouseId, CancellationToken cancellationToken)
        {
            var threshold = await _stockThresholdRepository.GetByProductAndWarehouseAsync(productId, warehouseId, cancellationToken);

            if (threshold is null)
            {
                return;
            }

            var totalQuantity = await _stockItemRepository.GetTotalQuantityAsync(productId, warehouseId, cancellationToken);

            if (totalQuantity < threshold.ReorderLevel)
            {
                return;
            }

            var resolveOperation = _lowStockAlertRepository.BuildResolveOperation(productId, warehouseId);
            var resolved = await UnitOfWork.ExecuteInTransactionAsync(resolveOperation, cancellationToken);

            if (resolved)
            {
                await _notificationPublisher.NotifyLowStockAlertAsync(
                    new NotificationServiceDTOs.LowStockAlertNotification(productId, warehouseId), cancellationToken);
            }
        }
    }
}