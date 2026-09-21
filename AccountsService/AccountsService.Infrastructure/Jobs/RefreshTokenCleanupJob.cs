using AccountsService.Application.Options;
using AccountsService.Application.Repositories.Interfaces;
using Microsoft.Extensions.Options;
using Quartz;
using Shared.Kernel.Caching;
using Shared.Kernel.Database;

namespace AccountsService.Infrastructure.Jobs
{
    public class RefreshTokenCleanupJob : IJob
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly IDistributedLock _distributedLock;
        private readonly IOptions<RefreshTokenCleanupJobSettings> _settings;

        public RefreshTokenCleanupJob(IUnitOfWork unitOfWork, IRefreshTokenRepository refreshTokenRepository,
            IDistributedLock distributedLock, IOptions<RefreshTokenCleanupJobSettings> settings)
        {
            _unitOfWork = unitOfWork;
            _refreshTokenRepository = refreshTokenRepository;
            _distributedLock = distributedLock;
            _settings = settings;
        }

        public async ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken = default)
        {
            var settingsValue = _settings.Value;

            var acquired = await _distributedLock.TryAcquireAsync(nameof(RefreshTokenCleanupJob),
                Environment.MachineName, TimeSpan.FromSeconds(settingsValue.LockTtlSeconds), cancellationToken);

            if (!acquired)
            {
                return;
            }

            var cutoff = DateTime.UtcNow.AddDays(-settingsValue.RetentionDays);

            await _unitOfWork.ExecuteInTransactionAsync(
                _refreshTokenRepository.BuildDeleteDeadOlderThanOperation(cutoff), cancellationToken);
        }
    }
}
