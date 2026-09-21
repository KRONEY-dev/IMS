using FluentValidation;
using InventoryService.Application.Exceptions;
using InventoryService.Application.Mappings;
using InventoryService.Application.Options;
using InventoryService.Application.Services;
using InventoryService.Application.Services.EventHandling;
using InventoryService.Application.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shared.Kernel.Exceptions;
using Shared.Kernel.Extensions;
using Shared.Kernel.Mapping;
using System.Reflection;

namespace InventoryService.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection InjectApplication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddSingleton<IExceptionMapperService, InventoryExceptionMapperSingletonService>();

            services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

            services.AddScoped<IWarehouseService, WarehouseScopedService>();
            services.AddScoped<IProductService, ProductScopedService>();
            services.AddScoped<ISupplierService, SupplierScopedService>();
            services.AddScoped<IStockService, StockScopedService>();
            services.AddScoped<IStockTransferService, StockTransferScopedService>();
            services.AddScoped<ISupplierOrderService, SupplierOrderScopedService>();
            services.AddScoped<ILowStockAlertService, LowStockAlertScopedService>();
            services.AddScoped<IOutboxMessageService, OutboxMessageScopedService>();

            services.AddScoped<IInventoryEventHandler, StockQuantityChangedEventHandler>();
            services.AddScoped<IInventoryEventHandlerFactory, InventoryEventHandlerFactory>();

            services.AddScoped<IMapperWrapper, AutoMapperWrapper>();
            services.AddAutoMapper(cfg =>
            {
                cfg.LicenseKey = configuration["AutoMapper:LicenseKey"];
                cfg.AddProfile<WarehouseMappingProfile>();
                cfg.AddProfile<ProductMappingProfile>();
                cfg.AddProfile<SupplierMappingProfile>();
                cfg.AddProfile<StockMappingProfile>();
                cfg.AddProfile<StockTransferMappingProfile>();
                cfg.AddProfile<SupplierOrderMappingProfile>();
                cfg.AddProfile<LowStockAlertMappingProfile>();
            });

            services.ConfigureOption<StockConcurrencySettings>(configuration);

            return services;
        }
    }
}