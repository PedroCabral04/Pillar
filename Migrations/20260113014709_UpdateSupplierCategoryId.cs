using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSupplierCategoryId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Suppliers");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Suppliers",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Suppliers_CategoryId",
                table: "Suppliers",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Suppliers_FinancialCategories_CategoryId",
                table: "Suppliers",
                column: "CategoryId",
                principalTable: "FinancialCategories",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Suppliers_FinancialCategories_CategoryId",
                table: "Suppliers");

            migrationBuilder.DropIndex(
                name: "IX_Suppliers_CategoryId",
                table: "Suppliers");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Suppliers");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Suppliers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }
    }
}
