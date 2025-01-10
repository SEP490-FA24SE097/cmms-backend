using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfigAndUpdateShippingDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Reason",
                table: "ShippingDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ShippingDetailStatus",
                table: "ShippingDetails",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ConfigCustomerDiscounts",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Customer = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Agency = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigCustomerDiscounts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigShippings",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    BaseFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    First5KmFree = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdditionalKmFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    First10KgFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AdditionalKgFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigShippings", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfigCustomerDiscounts");

            migrationBuilder.DropTable(
                name: "ConfigShippings");

            migrationBuilder.DropColumn(
                name: "Reason",
                table: "ShippingDetails");

            migrationBuilder.DropColumn(
                name: "ShippingDetailStatus",
                table: "ShippingDetails");
        }
    }
}
