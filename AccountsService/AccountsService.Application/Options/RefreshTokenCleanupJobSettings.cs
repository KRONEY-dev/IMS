using Shared.Kernel.Scheduling;

namespace AccountsService.Application.Options
{
    public class RefreshTokenCleanupJobSettings : ICronJobSettings
    {
        public string CronExpression { get; init; } = default!;
        public int RetentionDays { get; init; }
        public int LockTtlSeconds { get; init; }
    }
}
