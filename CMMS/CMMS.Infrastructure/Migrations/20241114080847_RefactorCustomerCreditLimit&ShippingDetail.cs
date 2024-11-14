using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class RefactorCustomerCreditLimitShippingDetail : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CustomerBalances_CustomerId",
                table: "CustomerBalances");

            migrationBuilder.AlterColumn<decimal>(
                name: "CreditLimit",
                table: "Users",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(double),
                oldType: "float",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Users",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CurrentDebt",
                table: "Users",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LimitCreditPurchaseTime",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDebt",
                table: "Users",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedById",
                table: "Transactions",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TransactionPaymentType",
                table: "Transactions",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneReceive",
                table: "ShippingDetails",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShipperId",
                table: "ShippingDetails",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TransactionPaymentType",
                table: "ShippingDetails",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Discount",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "SalePrice",
                table: "Invoices",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SellPlace",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "StaffId",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StoreId",
                table: "Invoices",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ShippingDetails_ShipperId",
                table: "ShippingDetails",
                column: "ShipperId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBalances_CustomerId",
                table: "CustomerBalances",
                column: "CustomerId");

            migrationBuilder.AddForeignKey(
                name: "FK_ShippingDetails_Users_ShipperId",
                table: "ShippingDetails",
                column: "ShipperId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
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

            migrationBuilder.DropIndex(
                name: "IX_CustomerBalances_CustomerId",
                table: "CustomerBalances");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CurrentDebt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LimitCreditPurchaseTime",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TotalDebt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "CreatedById",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "TransactionPaymentType",
                table: "Transactions");

            migrationBuilder.DropColumn(
                name: "PhoneReceive",
                table: "ShippingDetails");

            migrationBuilder.DropColumn(
                name: "ShipperId",
                table: "ShippingDetails");

            migrationBuilder.DropColumn(
                name: "TransactionPaymentType",
                table: "ShippingDetails");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SalePrice",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "SellPlace",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "StaffId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "StoreId",
                table: "Invoices");

            migrationBuilder.AlterColumn<double>(
                name: "CreditLimit",
                table: "Users",
                type: "float",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_CustomerBalances_CustomerId",
                table: "CustomerBalances",
                column: "CustomerId",
                unique: true);
        }
    }
}
