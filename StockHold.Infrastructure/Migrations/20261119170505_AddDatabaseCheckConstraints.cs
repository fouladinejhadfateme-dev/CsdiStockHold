using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StockHold.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseCheckConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_Reservations_ProductId_Status_ExpiresAt",
                table: "Reservations",
                newName: "IX_Reservations_Product_Status_ExpiresAt");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_CustomerId_IdempotencyKey",
                table: "Reservations",
                newName: "UX_Reservations_Customer_IdempotencyKey");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservations_Quantity",
                table: "Reservations",
                sql: "[Quantity] > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Reservations_Status",
                table: "Reservations",
                sql: "[Status] IN ('Active', 'Expired', 'Cancelled')");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Products_StockQuantity",
                table: "Products",
                sql: "[Quantity] >= 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Customers_CustomerType",
                table: "Customers",
                sql: "[CustomerType] IN ('Normal', 'Special')");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservations_Quantity",
                table: "Reservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Reservations_Status",
                table: "Reservations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Products_StockQuantity",
                table: "Products");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Customers_CustomerType",
                table: "Customers");

            migrationBuilder.RenameIndex(
                name: "UX_Reservations_Customer_IdempotencyKey",
                table: "Reservations",
                newName: "IX_Reservations_CustomerId_IdempotencyKey");

            migrationBuilder.RenameIndex(
                name: "IX_Reservations_Product_Status_ExpiresAt",
                table: "Reservations",
                newName: "IX_Reservations_ProductId_Status_ExpiresAt");
        }
    }
}
