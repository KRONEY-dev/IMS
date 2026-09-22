using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace InventoryService.Tests.Integration.Fixtures
{
    // Boots the real InventoryService.API host (real DI composition root, real SignalR hub,
    // real hosted services) against the shared Postgres/RabbitMQ/Redis Testcontainers instead
    // of mocking any of it - this is what makes group-membership tests trust the actual wiring.
    //
    // xUnit collection fixtures cannot depend on each other via constructor injection - only a
    // test class can receive multiple fixtures - so the connection info is set as properties by
    // the test class constructor, before anything touches Server/CreateClient() and triggers the
    // (lazy) host build.
    public class InventoryApiFactory : WebApplicationFactory<Program>
    {
        public string PostgresConnectionString { get; set; } = default!;
        public string RedisConnectionString { get; set; } = default!;
        public string RabbitMqHostName { get; set; } = default!;
        public int RabbitMqPort { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Inventory"] = PostgresConnectionString,
                    ["ConnectionStrings:Redis"] = RedisConnectionString,
                    ["RabbitMqSettings:HostName"] = RabbitMqHostName,
                    ["RabbitMqSettings:Port"] = RabbitMqPort.ToString(),
                    ["RabbitMqSettings:Queues:InventoryEvents"] = $"test-inventory-events-{Guid.NewGuid()}"
                });
            });
        }
    }
}
