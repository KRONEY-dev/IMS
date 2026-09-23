using InventoryService.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Registry;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace InventoryService.Infrastructure.Messaging
{
    public static class RabbitMqConnectionFactory
    {
        public const string ResiliencePipelineKey = "rabbitmq-connection";

        public static IServiceCollection AddRabbitMqConnectionResilience(this IServiceCollection services)
        {
            services.AddResiliencePipeline<string, IConnection>(ResiliencePipelineKey, (builder, context) =>
            {
                var settings = context.GetOptions<RabbitMqSettings>();

                builder.AddRetry(new RetryStrategyOptions<IConnection>
                {
                    MaxRetryAttempts = settings.ConnectionMaxRetryAttempts,
                    Delay = TimeSpan.FromSeconds(settings.ConnectionInitialRetryDelaySeconds),
                    MaxDelay = TimeSpan.FromSeconds(settings.ConnectionMaxRetryDelaySeconds),
                    BackoffType = DelayBackoffType.Exponential,
                    UseJitter = true,
                    ShouldHandle = new PredicateBuilder<IConnection>().Handle<BrokerUnreachableException>()
                });
            });

            return services;
        }

        public static ConnectionFactory Build(RabbitMqSettings settings)
        {
            return new ConnectionFactory { HostName = settings.HostName, Port = settings.Port };
        }

        public static async Task<IConnection> CreateAsync(
            RabbitMqSettings settings, ResiliencePipelineProvider<string> pipelineProvider, CancellationToken cancellationToken)
        {
            var connectionFactory = Build(settings);
            var pipeline = pipelineProvider.GetPipeline<IConnection>(ResiliencePipelineKey);

            return await pipeline.ExecuteAsync(
                ct => new ValueTask<IConnection>(connectionFactory.CreateConnectionAsync(ct)), cancellationToken);
        }
    }
}
