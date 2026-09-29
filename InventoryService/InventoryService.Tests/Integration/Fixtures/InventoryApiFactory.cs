using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace InventoryService.Tests.Integration.Fixtures
{
    public class InventoryApiFactory : WebApplicationFactory<Program>
    {
        public string PostgresConnectionString { get; set; } = default!;
        public string RedisConnectionString { get; set; } = default!;
        public string RabbitMqHostName { get; set; } = default!;
        public int RabbitMqPort { get; set; }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.UseSetting("ConnectionStrings:Inventory", PostgresConnectionString);
            builder.UseSetting("ConnectionStrings:Redis", RedisConnectionString);
            builder.UseSetting("RabbitMqSettings:HostName", RabbitMqHostName);
            builder.UseSetting("RabbitMqSettings:Port", RabbitMqPort.ToString());
            builder.UseSetting("RabbitMqSettings:Queues:InventoryEvents", $"test-inventory-events-{Guid.NewGuid()}");
        }
    }
}