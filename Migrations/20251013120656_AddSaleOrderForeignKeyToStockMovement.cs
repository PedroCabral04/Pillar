using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class AddSaleOrderForeignKeyToStockMovement : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_StockMovements_SaleOrderId",
                table: "StockMovements",
                column: "SaleOrderId");

            migrationBuilder.AddForeignKey(
                name: "FK_StockMovements_Sales_SaleOrderId",
                table: "StockMovements",
                column: "SaleOrderId",
                principalTable: "Sales",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_StockMovements_Sales_SaleOrderId",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "IX_StockMovements_SaleOrderId",
                table: "StockMovements");
        }
    }
}
