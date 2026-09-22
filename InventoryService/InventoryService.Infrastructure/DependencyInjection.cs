using InventoryService.Application.Options;
using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Application.Services.Interfaces;
using InventoryService.Infrastructure.Database;
using InventoryService.Infrastructure.Database.Repositories;
using InventoryService.Infrastructure.Jobs;
using InventoryService.Infrastructure.Messaging;
using InventoryService.Infrastructure.Options;
using InventoryService.Infrastructure.RealTime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.AspNetCore.Requests;
using Shared.Kernel.AspNetCore.Requests.Middleware;
using Shared.Kernel.Database;
using Shared.Kernel.Extensions;
using Shared.Kernel.Quartz;
using Shared.Kernel.Redis;
using Shared.Kernel.Requests;

namespace InventoryService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection InjectInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            ConfigureOptions(services, configuration);
            AddServices(services, configuration);
            AddQuartzJobs(services, configuration);

            return services;
        }

        public static void UseExceptionHandling(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
        }

        public static void UseRequestContext(this IApplicationBuilder app)
        {
            app.UseMiddleware<RequestContextMiddleware>();
        }

        public static void MapInventoryHub(this IEndpointRouteBuilder app)
        {
            app.MapHub<InventoryHub>("/hubs/inventory");
        }

        private static void AddServices(IServiceCollection services, IConfiguration configuration)
        {
            services.AddDbContext<InventoryDbContext>(options =>
                options.UseNpgsql(configuration.GetConnectionString("Inventory")),
                optionsLifetime: ServiceLifetime.Singleton);

            services.AddDbContextFactory<InventoryDbContext>();

            services.AddScoped<IUnitOfWork, UnitOfWorkScoped>();
            services.AddScoped<IWarehouseRepository, WarehouseRepositoryScoped>();
            services.AddScoped<IProductRepository, ProductRepositoryScoped>();
            services.AddScoped<ISupplierRepository, SupplierRepositoryScoped>();
            services.AddScoped<IStockThresholdRepository, StockThresholdRepositoryScoped>();
            services.AddScoped<IStockItemRepository, StockItemRepositoryScoped>();
            services.AddScoped<IStockMovementRepository, StockMovementRepositoryScoped>();
            services.AddScoped<IStockTransferRepository, StockTransferRepositoryScoped>();
            services.AddScoped<ISupplierOrderRepository, SupplierOrderRepositoryScoped>();
            services.AddScoped<ISupplierOrderItemRepository, SupplierOrderItemRepositoryScoped>();
            services.AddScoped<ILowStockAlertRepository, LowStockAlertRepositoryScoped>();
            services.AddScoped<IOutboxMessageRepository, OutboxMessageRepositoryScoped>();

            services.AddScoped<IRequestContext, RequestContextScoped>();

            services.AddHostedService<RabbitMqOutboxPublisherHostedService>();
            services.AddHostedService<RabbitMqConsumerHostedService>();

            services.AddRedisDistributedLock(configuration);

            services.AddSignalR().AddStackExchangeRedis(configuration.GetConnectionString("Redis")!);
            services.AddScoped<INotificationPublisher, SignalRNotificationPublisher>();

            services.AddSingleton<IUserConnectionRegistry, RedisUserConnectionRegistry>();
            services.AddHostedService<UserAccessChangeSubscriberHostedService>();
        }

        private static void AddQuartzJobs(IServiceCollection services, IConfiguration configuration)
        {
            services.AddQuartzWithCronJobs(quartz =>
            {
                quartz.AddCronJob<SupplierOrderReminderJob, SupplierOrderReminderJobSettings>(configuration);
            });
        }

        private static void ConfigureOptions(IServiceCollection services, IConfiguration configuration)
        {
            services.ConfigureOption<RabbitMqSettings>(configuration);
            services.ConfigureOption<SupplierOrderReminderJobSettings>(configuration);
        }
    }
}