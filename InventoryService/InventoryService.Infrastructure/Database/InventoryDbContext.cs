using InventoryService.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace InventoryService.Infrastructure.Database
{
    public class InventoryDbContext(DbContextOptions<InventoryDbContext> options) : DbContext(options)
    {
        public DbSet<Warehouse> Warehouses
        {
            get
            {
                return Set<Warehouse>();
            }
        }

        public DbSet<Product> Products
        {
            get
            {
                return Set<Product>();
            }
        }

        public DbSet<StockItem> StockItems
        {
            get
            {
                return Set<StockItem>();
            }
        }

        public DbSet<StockThreshold> StockThresholds
        {
            get
            {
                return Set<StockThreshold>();
            }
        }

        public DbSet<StockMovement> StockMovements
        {
            get
            {
                return Set<StockMovement>();
            }
        }

        public DbSet<StockTransfer> StockTransfers
        {
            get
            {
                return Set<StockTransfer>();
            }
        }

        public DbSet<Supplier> Suppliers
        {
            get
            {
                return Set<Supplier>();
            }
        }

        public DbSet<SupplierOrder> SupplierOrders
        {
            get
            {
                return Set<SupplierOrder>();
            }
        }

        public DbSet<SupplierOrderItem> SupplierOrderItems
        {
            get
            {
                return Set<SupplierOrderItem>();
            }
        }

        public DbSet<LowStockAlert> LowStockAlerts
        {
            get
            {
                return Set<LowStockAlert>();
            }
        }

        public DbSet<OutboxMessage> OutboxMessages
        {
            get
            {
                return Set<OutboxMessage>();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(InventoryDbContext).Assembly);
        }
    }
}