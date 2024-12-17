using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddImportStoreId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "StoreId",
                table: "Imports",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Imports_StoreId",
                table: "Imports",
                column: "StoreId");

            migrationBuilder.AddForeignKey(
                name: "FK_Imports_Stores_StoreId",
                table: "Imports",
                column: "StoreId",
                principalTable: "Stores",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Imports_Stores_StoreId",
                table: "Imports");

            migrationBuilder.DropIndex(
                name: "IX_Imports_StoreId",
                table: "Imports");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Imports");
        }
    }
}
