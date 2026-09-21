using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace InventoryService.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddShipmentIdToStockTransfer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ShipmentId",
                table: "StockTransfers",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_StockTransfers_ShipmentId",
                table: "StockTransfers",
                column: "ShipmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_StockTransfers_ShipmentId",
                table: "StockTransfers");

            migrationBuilder.DropColumn(
                name: "ShipmentId",
                table: "StockTransfers");
        }
    }
}
