using InventoryService.Application.Options;
using InventoryService.Application.Repositories.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using Shared.Kernel.Caching;

namespace InventoryService.Infrastructure.Jobs
{
    public class SupplierOrderReminderJob : IJob
    {
        private readonly ISupplierOrderRepository _supplierOrderRepository;
        private readonly IDistributedLock _distributedLock;
        private readonly IOptions<SupplierOrderReminderJobSettings> _settings;
        private readonly ILogger<SupplierOrderReminderJob> _logger;

        public SupplierOrderReminderJob(ISupplierOrderRepository supplierOrderRepository, IDistributedLock distributedLock,
            IOptions<SupplierOrderReminderJobSettings> settings, ILogger<SupplierOrderReminderJob> logger)
        {
            _supplierOrderRepository = supplierOrderRepository;
            _distributedLock = distributedLock;
            _settings = settings;
            _logger = logger;
        }

        public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var settingsValue = _settings.Value;

            var acquired = await _distributedLock.TryAcquireAsync(nameof(SupplierOrderReminderJob),
                Environment.MachineName, TimeSpan.FromSeconds(settingsValue.LockTtlSeconds), cancellationToken);

            if (!acquired)
            {
                return;
            }

            var cutoff = DateTime.UtcNow.AddDays(settingsValue.LookaheadDays);
            var arrivingOrders = await _supplierOrderRepository.GetArrivingByAsync(cutoff, cancellationToken);

            // TODO: replace this log with a real push notification (e.g. SignalR) once one exists for this event.
            foreach (var order in arrivingOrders)
            {
                _logger.LogInformation(
                    "SupplierOrder {SupplierOrderId} for warehouse {WarehouseId} is expected by {ExpectedDeliveryDate}",
                    order.Id, order.WarehouseId, order.ExpectedDeliveryDate);
            }
        }
    }
}
