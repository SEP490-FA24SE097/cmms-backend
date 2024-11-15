using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateShippingDetailsCustomer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShippingDetails_ShipperId",
                table: "ShippingDetails");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingDetails_ShipperId",
                table: "ShippingDetails",
                column: "ShipperId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ShippingDetails_ShipperId",
                table: "ShippingDetails");

            migrationBuilder.CreateIndex(
                name: "IX_ShippingDetails_ShipperId",
                table: "ShippingDetails",
                column: "ShipperId",
                unique: true,
                filter: "[ShipperId] IS NOT NULL");
        }
    }
}
