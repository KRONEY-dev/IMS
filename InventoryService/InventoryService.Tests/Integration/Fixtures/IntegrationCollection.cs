using Xunit;

namespace InventoryService.Tests.Integration.Fixtures
{
    [CollectionDefinition(Name)]
    public class IntegrationCollection :
        ICollectionFixture<PostgresContainerFixture>,
        ICollectionFixture<RabbitMqContainerFixture>,
        ICollectionFixture<RedisContainerFixture>,
        ICollectionFixture<ApiHostPostgresContainerFixture>,
        ICollectionFixture<InventoryApiFactory>
    {
        public const string Name = "Integration";
    }
}
