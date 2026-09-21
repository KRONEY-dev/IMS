using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class ChangeStockItemBatchUniqueIndexToIncludeProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockItems_WarehouseId_BatchId",
                table: "StockItems");

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_WarehouseId_ProductId_BatchId",
                table: "StockItems",
                columns: new[] { "WarehouseId", "ProductId", "BatchId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockItems_WarehouseId_ProductId_BatchId",
                table: "StockItems");

            migrationBuilder.CreateIndex(
                name: "IX_StockItems_WarehouseId_BatchId",
                table: "StockItems",
                columns: new[] { "WarehouseId", "BatchId" },
                unique: true);
        }
    }
}
