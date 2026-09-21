using Shared.Kernel.Scheduling;

namespace InventoryService.Application.Options
{
    public class SupplierOrderReminderJobSettings : ICronJobSettings
    {
        public string CronExpression { get; init; } = default!;
        public int LookaheadDays { get; init; }
        public int LockTtlSeconds { get; init; }
    }
}
