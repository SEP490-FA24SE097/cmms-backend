using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateMaterial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Suppliers_SupplierID",
                table: "Materials");

            migrationBuilder.RenameColumn(
                name: "SupplierID",
                table: "Materials",
                newName: "SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_Materials_SupplierID",
                table: "Materials",
                newName: "IX_Materials_SupplierId");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Suppliers_SupplierId",
                table: "Materials",
                column: "SupplierId",
                principalTable: "Suppliers",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Materials_Suppliers_SupplierId",
                table: "Materials");

            migrationBuilder.RenameColumn(
                name: "SupplierId",
                table: "Materials",
                newName: "SupplierID");

            migrationBuilder.RenameIndex(
                name: "IX_Materials_SupplierId",
                table: "Materials",
                newName: "IX_Materials_SupplierID");

            migrationBuilder.AddForeignKey(
                name: "FK_Materials_Suppliers_SupplierID",
                table: "Materials",
                column: "SupplierID",
                principalTable: "Suppliers",
                principalColumn: "ID",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
