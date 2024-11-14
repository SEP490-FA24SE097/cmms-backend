using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Variants_ConversionUnitId",
                table: "Variants",
                column: "ConversionUnitId");

            migrationBuilder.AddForeignKey(
                name: "FK_Variants_ConversionUnits_ConversionUnitId",
                table: "Variants",
                column: "ConversionUnitId",
                principalTable: "ConversionUnits",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Variants_ConversionUnits_ConversionUnitId",
                table: "Variants");

            migrationBuilder.DropIndex(
                name: "IX_Variants_ConversionUnitId",
                table: "Variants");
        }
    }
}
