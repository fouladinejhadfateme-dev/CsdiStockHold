using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockHold.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ChangeCustomerTypeToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Customers_CustomerType",
                table: "Customers");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerType",
                table: "Customers",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customers_CustomerType",
                table: "Customers",
                sql: "[CustomerType] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Customers_CustomerType",
                table: "Customers");

            migrationBuilder.AlterColumn<string>(
                name: "CustomerType",
                table: "Customers",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customers_CustomerType",
                table: "Customers",
                sql: "[CustomerType] IN ('Normal', 'Special')");
        }
    }
}
