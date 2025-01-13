using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CMMS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class updateIsRefundField : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsRefunded",
                table: "Invoices",
                type: "bit",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRefunded",
                table: "Invoices");
        }
    }
}
