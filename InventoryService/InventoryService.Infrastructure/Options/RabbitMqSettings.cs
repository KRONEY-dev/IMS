namespace InventoryService.Infrastructure.Options
{
    public class RabbitMqSettings
    {
        public string HostName { get; init; } = default!;
        public int BatchSize { get; init; }
        public int PollingIntervalSeconds { get; init; }
        public Dictionary<string, string> Queues { get; init; } = new();
    }
}
