namespace InventoryService.Infrastructure.Options
{
    public class RabbitMqSettings
    {
        public string HostName { get; init; } = default!;
        public int Port { get; init; }
        public int BatchSize { get; init; }
        public int PollingIntervalSeconds { get; init; }
        public int ConnectionMaxRetryAttempts { get; init; }
        public double ConnectionInitialRetryDelaySeconds { get; init; }
        public double ConnectionMaxRetryDelaySeconds { get; init; }
        public Dictionary<string, string> Queues { get; init; } = new();
    }
}
