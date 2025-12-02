using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class _20251118121500_AddTenantDatabaseNameUniqueIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Tenants_DatabaseName",
                table: "Tenants",
                column: "DatabaseName",
                unique: true,
                filter: "\"DatabaseName\" IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_DatabaseName",
                table: "Tenants");
        }
    }
}
