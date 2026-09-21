using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AccountsService.Infrastructure.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenamePrimaryKeysToId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "Users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "TokenId",
                table: "RefreshTokens",
                newName: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "Users",
                newName: "UserId");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "RefreshTokens",
                newName: "TokenId");
        }
    }
}
