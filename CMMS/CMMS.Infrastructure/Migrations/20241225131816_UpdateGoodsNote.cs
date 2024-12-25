using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UpdateGoodsNote : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodsDeliveryNoteDetails");

            migrationBuilder.DropTable(
                name: "GoodsDeliveryNotes");

            migrationBuilder.CreateTable(
                name: "GoodsNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReasonDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TotalQuantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Type = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsNotes_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "GoodsNoteDetails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    GoodsDeliveryNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MaterialId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    VariantId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GoodsNoteId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsNoteDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsNoteDetails_GoodsNotes_GoodsNoteId",
                        column: x => x.GoodsNoteId,
                        principalTable: "GoodsNotes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsNoteDetails_Materials_MaterialId",
                        column: x => x.MaterialId,
                        principalTable: "Materials",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GoodsNoteDetails_Variants_VariantId",
                        column: x => x.VariantId,
                        principalTable: "Variants",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_GoodsNoteDetails_GoodsNoteId",
                table: "GoodsNoteDetails",
                column: "GoodsNoteId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsNoteDetails_MaterialId",
                table: "GoodsNoteDetails",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsNoteDetails_VariantId",
                table: "GoodsNoteDetails",
                column: "VariantId");

            migrationBuilder.CreateIndex(
                name: "IX_GoodsNotes_StoreId",
                table: "GoodsNotes",
                column: "StoreId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GoodsNoteDetails");

            migrationBuilder.DropTable(
                name: "GoodsNotes");

            migrationBuilder.CreateTable(
                name: "GoodsDeliveryNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StoreId = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ReasonDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TimeStamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalByText = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GoodsDeliveryNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GoodsDeliveryNotes_Stores_StoreId",
                        column: x => x.StoreId,
                        principalTable: "Stores",
                        principalColumn: "Id");
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
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false)
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

            migrationBuilder.CreateIndex(
                name: "IX_GoodsDeliveryNotes_StoreId",
                table: "GoodsDeliveryNotes",
                column: "StoreId");
        }
    }
}
