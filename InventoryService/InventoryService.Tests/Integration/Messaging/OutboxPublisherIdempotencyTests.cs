using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Domain.Entities;
using InventoryService.Infrastructure.Database;
using InventoryService.Infrastructure.Messaging;
using InventoryService.Infrastructure.Options;
using InventoryService.Tests.Integration.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Polly.Registry;
using RabbitMQ.Client;
using Shared.Kernel.Database;
using Shared.Kernel.Extensions;
using Xunit;

namespace InventoryService.Tests.Integration.Messaging
{
    [Trait("Category", "Integration")]
    [Collection(IntegrationCollection.Name)]
    public class OutboxPublisherIdempotencyTests
    {
        private readonly PostgresContainerFixture _postgres;
        private readonly RabbitMqContainerFixture _rabbitMq;

        public OutboxPublisherIdempotencyTests(PostgresContainerFixture postgres, RabbitMqContainerFixture rabbitMq)
        {
            _postgres = postgres;
            _rabbitMq = rabbitMq;
        }

        [Fact]
        public async Task MarkProcessed_CalledTwice_SecondCallIsNoOp()
        {
            var messageId = await SeedUnprocessedMessageAsync();

            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var firstCallSucceeded = await unitOfWork.ExecuteInTransactionAsync(
                repository.BuildMarkProcessedOperation(messageId), CancellationToken.None);

            var secondCallSucceeded = await unitOfWork.ExecuteInTransactionAsync(
                repository.BuildMarkProcessedOperation(messageId), CancellationToken.None);

            Assert.True(firstCallSucceeded);
            Assert.False(secondCallSucceeded);

            var processedAt = await GetProcessedAtAsync(messageId);
            Assert.NotNull(processedAt);
        }

        [Fact]
        public async Task PublisherHostedService_PublishesEachUnprocessedMessageExactlyOnce()
        {
            var firstMessageId = await SeedUnprocessedMessageAsync();
            var secondMessageId = await SeedUnprocessedMessageAsync();

            var queueName = $"test-outbox-{Guid.NewGuid()}";
            var settingsValue = new RabbitMqSettings
            {
                HostName = _rabbitMq.Hostname,
                Port = _rabbitMq.Port,
                BatchSize = 10,
                PollingIntervalSeconds = 1,
                ConnectionMaxRetryAttempts = 3,
                ConnectionInitialRetryDelaySeconds = 1,
                ConnectionMaxRetryDelaySeconds = 5,
                Queues = { [RabbitMqQueueNames.InventoryEvents] = queueName }
            };
            var settings = Options.Create(settingsValue);

            var retryConfiguration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RabbitMqSettings:HostName"] = settingsValue.HostName,
                ["RabbitMqSettings:Port"] = settingsValue.Port.ToString(),
                ["RabbitMqSettings:ConnectionMaxRetryAttempts"] = settingsValue.ConnectionMaxRetryAttempts.ToString(),
                ["RabbitMqSettings:ConnectionInitialRetryDelaySeconds"] = settingsValue.ConnectionInitialRetryDelaySeconds.ToString(),
                ["RabbitMqSettings:ConnectionMaxRetryDelaySeconds"] = settingsValue.ConnectionMaxRetryDelaySeconds.ToString()
            }).Build();

            var resilienceServices = new ServiceCollection();
            resilienceServices.ConfigureOption<RabbitMqSettings>(retryConfiguration);
            resilienceServices.AddRabbitMqConnectionResilience();
            var pipelineProvider = resilienceServices.BuildServiceProvider()
                .GetRequiredService<ResiliencePipelineProvider<string>>();

            var publisher = new RabbitMqOutboxPublisherHostedService(_postgres.ScopeFactory, settings, pipelineProvider);

            await publisher.StartAsync(CancellationToken.None);

            try
            {
                await WaitUntilAsync(async () =>
                {
                    var firstProcessedAt = await GetProcessedAtAsync(firstMessageId);
                    var secondProcessedAt = await GetProcessedAtAsync(secondMessageId);

                    return firstProcessedAt is not null && secondProcessedAt is not null;
                }, TimeSpan.FromSeconds(15));
            }
            finally
            {
                await publisher.StopAsync(CancellationToken.None);
                publisher.Dispose();
            }

            var publishedCount = await CountQueueMessagesAsync(queueName);
            Assert.Equal(2, publishedCount);
        }

        private async Task<Guid> SeedUnprocessedMessageAsync()
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            var message = OutboxMessage.Create("TestEvent", "{}");
            dbContext.OutboxMessages.Add(message);
            await dbContext.SaveChangesAsync();

            return message.Id;
        }

        private async Task<DateTime?> GetProcessedAtAsync(Guid messageId)
        {
            await using var scope = _postgres.ScopeFactory.CreateAsyncScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<InventoryDbContext>();

            return await dbContext.OutboxMessages
                .Where(message => message.Id == messageId)
                .Select(message => message.ProcessedAt)
                .SingleAsync();
        }

        private async Task<int> CountQueueMessagesAsync(string queueName)
        {
            using var connection = await _rabbitMq.CreateConnectionAsync();
            using var channel = await connection.CreateChannelAsync();

            await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);

            var count = 0;

            while (await channel.BasicGetAsync(queueName, autoAck: true) is not null)
            {
                count++;
            }

            return count;
        }

        private static async Task WaitUntilAsync(Func<Task<bool>> condition, TimeSpan timeout)
        {
            var deadline = DateTime.UtcNow + timeout;

            while (DateTime.UtcNow < deadline)
            {
                if (await condition())
                {
                    return;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(200));
            }

            throw new TimeoutException($"Condition was not met within {timeout}.");
        }
    }
}
