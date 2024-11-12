using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInvoiceDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceDetailId",
                table: "Stores",
                type: "nvarchar(450)",
                nullable: true);

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

            migrationBuilder.AddForeignKey(
                name: "FK_Stores_InvoiceDetails_InvoiceDetailId",
                table: "Stores",
                column: "InvoiceDetailId",
                principalTable: "InvoiceDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Stores_InvoiceDetails_InvoiceDetailId",
                table: "Stores");

            migrationBuilder.DropIndex(
                name: "IX_Stores_InvoiceDetailId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "InvoiceDetailId",
                table: "Stores");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "InvoiceDetails");
        }
    }
}
