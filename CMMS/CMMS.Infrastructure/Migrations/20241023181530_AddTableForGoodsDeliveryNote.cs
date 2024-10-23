using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTableForGoodsDeliveryNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GoodsDeliveryNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ReasonDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalByText = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsDeliveryNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GoodsDeliveryNoteDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsDeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsDeliveryNoteDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsDeliveryNoteDetails_GoodsDeliveryNotes_GoodsDeliveryNoteId",
                        column: x => x.GoodsDeliveryNoteId,
                        principalTable: "GoodsDeliveryNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsDeliveryNoteDetails_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsDeliveryNoteDetails_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsDeliveryNoteDetails_GoodsDeliveryNoteId",
                table: "GoodsDeliveryNoteDetails",
                column: "GoodsDeliveryNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsDeliveryNoteDetails_MaterialId",
                table: "GoodsDeliveryNoteDetails",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsDeliveryNoteDetails_VariantId",
                table: "GoodsDeliveryNoteDetails",
                column: "VariantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodsDeliveryNoteDetails");

            migrationBuilder.DropTable(
                name: "GoodsDeliveryNotes");
        }
    }
}
