using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations.DesignTime
{
    /// <inheritdoc />
    public partial class AddPerformanceIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Product search optimization
            migrationBuilder.CreateIndex(
                name: "idx_products_search",
                table: "Products",
                columns: new[] { "Name", "Sku" });

            // Stock movement queries
            migrationBuilder.CreateIndex(
                name: "idx_stock_movements_product_date",
                table: "StockMovements",
                columns: new[] { "ProductId", "MovementDate" });

            // Chatbot conversation queries
            migrationBuilder.CreateIndex(
                name: "idx_chat_conversations_user_date",
                table: "ChatConversations",
                columns: new[] { "UserId", "LastMessageAt" });

            // Financial dashboard date range queries
            migrationBuilder.CreateIndex(
                name: "idx_accounts_payable_issue_date",
                table: "AccountsPayable",
                column: "IssueDate");

            migrationBuilder.CreateIndex(
                name: "idx_accounts_receivable_issue_date",
                table: "AccountsReceivable",
                column: "IssueDate");

            // Sales queries
            migrationBuilder.CreateIndex(
                name: "idx_sales_date_status",
                table: "Sales",
                columns: new[] { "SaleDate", "Status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_products_search",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "idx_stock_movements_product_date",
                table: "StockMovements");

            migrationBuilder.DropIndex(
                name: "idx_chat_conversations_user_date",
                table: "ChatConversations");

            migrationBuilder.DropIndex(
                name: "idx_accounts_payable_issue_date",
                table: "AccountsPayable");

            migrationBuilder.DropIndex(
                name: "idx_accounts_receivable_issue_date",
                table: "AccountsReceivable");

            migrationBuilder.DropIndex(
                name: "idx_sales_date_status",
                table: "Sales");
        }
    }
}
