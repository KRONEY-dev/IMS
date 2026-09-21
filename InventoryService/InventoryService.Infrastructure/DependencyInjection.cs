using InventoryService.Application.Repositories.Interfaces;
using InventoryService.Infrastructure.Database;
using InventoryService.Infrastructure.Database.Repositories;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.AspNetCore.Requests;
using Shared.Kernel.AspNetCore.Requests.Middleware;
using Shared.Kernel.Database;
using Shared.Kernel.Requests;

namespace InventoryService.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection InjectInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            AddServices(services, configuration);

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

            services.AddScoped<IRequestContext, RequestContextScoped>();
        }
    }
}