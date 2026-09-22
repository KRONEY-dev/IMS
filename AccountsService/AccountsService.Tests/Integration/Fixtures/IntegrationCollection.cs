using Xunit;

namespace AccountsService.Tests.Integration.Fixtures
{
    [CollectionDefinition(Name)]
    public class IntegrationCollection : ICollectionFixture<PostgresContainerFixture>
    {
        public const string Name = "Integration";
    }
}
