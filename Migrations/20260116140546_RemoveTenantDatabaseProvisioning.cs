using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTenantDatabaseProvisioning : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tenants_DatabaseName",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "ConnectionString",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DatabaseName",
                table: "Tenants");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConnectionString",
                table: "Tenants",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DatabaseName",
                table: "Tenants",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_DatabaseName",
                table: "Tenants",
                column: "DatabaseName",
                unique: true,
                filter: "\"DatabaseName\" IS NOT NULL");
        }
    }
}
