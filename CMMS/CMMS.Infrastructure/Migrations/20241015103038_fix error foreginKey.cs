using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class fixerrorforeginKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Materials_MaterialsId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Variants_VariantsId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceDetails_Variants_VariantsId",
                table: "InvoiceDetails");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceDetails_VariantsId",
                table: "InvoiceDetails");

            migrationBuilder.DropIndex(
                name: "IX_Carts_MaterialsId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Carts_VariantsId",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "VariantsId",
                table: "InvoiceDetails");

            migrationBuilder.DropColumn(
                name: "MaterialsId",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "VariantsId",
                table: "Carts");

            migrationBuilder.AddColumn<string>(
                name: "InvoiceDetailId",
                table: "Variants",
                type: "nvarchar(450)",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "VariantId",
                table: "InvoiceDetails",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MaterialId",
                table: "InvoiceDetails",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<Guid>(
                name: "VariantId",
                table: "Carts",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "MaterialId",
                table: "Carts",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.CreateIndex(
                name: "IX_Variants_InvoiceDetailId",
                table: "Variants",
                column: "InvoiceDetailId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_MaterialId",
                table: "Carts",
                column: "MaterialId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_VariantId",
                table: "Carts",
                column: "VariantId");

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Materials_MaterialId",
                table: "Carts",
                column: "MaterialId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Variants_VariantId",
                table: "Carts",
                column: "VariantId",
                principalTable: "Variants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Variants_InvoiceDetails_InvoiceDetailId",
                table: "Variants",
                column: "InvoiceDetailId",
                principalTable: "InvoiceDetails",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Materials_MaterialId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_Carts_Variants_VariantId",
                table: "Carts");

            migrationBuilder.DropForeignKey(
                name: "FK_Variants_InvoiceDetails_InvoiceDetailId",
                table: "Variants");

            migrationBuilder.DropIndex(
                name: "IX_Variants_InvoiceDetailId",
                table: "Variants");

            migrationBuilder.DropIndex(
                name: "IX_Carts_MaterialId",
                table: "Carts");

            migrationBuilder.DropIndex(
                name: "IX_Carts_VariantId",
                table: "Carts");

            migrationBuilder.DropColumn(
                name: "InvoiceDetailId",
                table: "Variants");

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<string>(
                name: "VariantId",
                table: "InvoiceDetails",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MaterialId",
                table: "InvoiceDetails",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "VariantsId",
                table: "InvoiceDetails",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "VariantId",
                table: "Carts",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "MaterialId",
                table: "Carts",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "MaterialsId",
                table: "Carts",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<Guid>(
                name: "VariantsId",
                table: "Carts",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceDetails_VariantsId",
                table: "InvoiceDetails",
                column: "VariantsId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_MaterialsId",
                table: "Carts",
                column: "MaterialsId");

            migrationBuilder.CreateIndex(
                name: "IX_Carts_VariantsId",
                table: "Carts",
                column: "VariantsId");

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Materials_MaterialsId",
                table: "Carts",
                column: "MaterialsId",
                principalTable: "Materials",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Carts_Variants_VariantsId",
                table: "Carts",
                column: "VariantsId",
                principalTable: "Variants",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceDetails_Variants_VariantsId",
                table: "InvoiceDetails",
                column: "VariantsId",
                principalTable: "Variants",
                principalColumn: "Id");
        }
    }
}
