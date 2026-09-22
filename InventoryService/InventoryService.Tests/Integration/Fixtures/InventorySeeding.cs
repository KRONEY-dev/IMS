using InventoryService.Domain.Entities;
using InventoryService.Domain.Entities.ValueObjects;
using InventoryService.Infrastructure.Database;

namespace InventoryService.Tests.Integration.Fixtures
{
    // Shared arrange-phase helper for integration tests: seeds a warehouse/product pair with
    // a reorder threshold and a stock quantity already below it, so LowStockAlertScopedService
    // has something real to evaluate against. Bypasses repositories/IUnitOfWork on purpose -
    // this is test data setup, not the behavior under test.
    public static class InventorySeeding
    {
        public static async Task<(Guid ProductId, Guid WarehouseId)> SeedBelowThresholdAsync(
            InventoryDbContext dbContext, int reorderLevel = 10, int quantity = 5)
        {
            var warehouse = Warehouse.Create($"Test warehouse {Guid.NewGuid()}", "Test location", FullWeekWorkingHours());
            var product = Product.Create("Test product", "Test category", null);

            var threshold = StockThreshold.Create(product.Id, warehouse.Id, reorderLevel, reorderQuantity: 1);
            var stockItem = StockItem.Create(product.Id, warehouse.Id, Guid.NewGuid(), quantity, price: 1m);

            dbContext.Warehouses.Add(warehouse);
            dbContext.Products.Add(product);
            dbContext.StockThresholds.Add(threshold);
            dbContext.StockItems.Add(stockItem);

            await dbContext.SaveChangesAsync();

            return (product.Id, warehouse.Id);
        }

        public static async Task<(Guid ProductId, Guid WarehouseId)> SeedProductAndWarehouseAsync(InventoryDbContext dbContext)
        {
            var warehouse = Warehouse.Create($"Test warehouse {Guid.NewGuid()}", "Test location", FullWeekWorkingHours());
            var product = Product.Create("Test product", "Test category", null);

            dbContext.Warehouses.Add(warehouse);
            dbContext.Products.Add(product);

            await dbContext.SaveChangesAsync();

            return (product.Id, warehouse.Id);
        }

        public static async Task<Guid> SeedSubmittedSupplierOrderAsync(
            InventoryDbContext dbContext, DateTime? expectedDeliveryDate)
        {
            var warehouse = Warehouse.Create($"Test warehouse {Guid.NewGuid()}", "Test location", FullWeekWorkingHours());
            var supplier = Supplier.Create($"Test supplier {Guid.NewGuid()}", null, null);

            var order = SupplierOrder.Create(supplier.Id, warehouse.Id, Guid.NewGuid());
            order.Submit(expectedDeliveryDate);

            dbContext.Warehouses.Add(warehouse);
            dbContext.Suppliers.Add(supplier);
            dbContext.SupplierOrders.Add(order);

            await dbContext.SaveChangesAsync();

            return order.Id;
        }

        private static List<WorkingHours> FullWeekWorkingHours()
        {
            return Enum.GetValues<DayOfWeek>()
                .Select(day => new WorkingHours(day, false, new TimeOnly(0, 0), new TimeOnly(0, 0)))
                .ToList();
        }
    }
}
