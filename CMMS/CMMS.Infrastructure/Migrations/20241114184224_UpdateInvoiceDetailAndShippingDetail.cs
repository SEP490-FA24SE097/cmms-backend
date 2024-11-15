using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInvoiceDetailAndShippingDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShippingDetails_Users_ShipperId",
                table: "ShippingDetails");

            migrationBuilder.DropForeignKey(
                name: "FK_Stores_InvoiceDetails_InvoiceDetailId",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_Stores_InvoiceDetailId",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_ShippingDetails_ShipperId",
                table: "ShippingDetails");

            migrationBuilder.DropColumn(
                name: "TotalDebt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "InvoiceDetailId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "InvoiceDetails");

            migrationBuilder.AlterColumn<string>(
                name: "ShipperId",
                table: "ShippingDetails",
                type: "nvarchar(450)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingDetails_ShipperId",
                table: "ShippingDetails",
                column: "ShipperId",
                unique: true,
                filter: "[ShipperId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingDetails_Users_ShipperId",
                table: "ShippingDetails",
                column: "ShipperId",
                principalTable: "Users",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ShippingDetails_Users_ShipperId",
                table: "ShippingDetails");

            migrationBuilder.DropIndex(
                name: "IX_ShippingDetails_ShipperId",
                table: "ShippingDetails");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDebt",
                table: "Users",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceDetailId",
                table: "Stores",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ShipperId",
                table: "ShippingDetails",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreId",
                table: "InvoiceDetails",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Stores_InvoiceDetailId",
                table: "Stores",
                column: "InvoiceDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingDetails_ShipperId",
                table: "ShippingDetails",
                column: "ShipperId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingDetails_Users_ShipperId",
                table: "ShippingDetails",
                column: "ShipperId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Stores_InvoiceDetails_InvoiceDetailId",
                table: "Stores",
                column: "InvoiceDetailId",
                principalTable: "InvoiceDetails",
                principalColumn: "Id");
        }
    }
}
