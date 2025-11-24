using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations
{
    public partial class AddTenantIdToIdentity : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add TenantId columns (nullable) to AspNetUsers and AspNetRoles if not present
            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AspNetUsers",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AspNetRoles",
                type: "integer",
                nullable: true);

            // Create indexes
            migrationBuilder.CreateIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoles_TenantId",
                table: "AspNetRoles",
                column: "TenantId");

            // Conditionally add FK to Tenants if the table exists (safe for different deploy orders)
            var addFkSql = @"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.tables WHERE table_name = 'tenants') THEN
    IF NOT EXISTS (
      SELECT 1 FROM information_schema.table_constraints
      WHERE constraint_name = 'FK_AspNetUsers_Tenants_TenantId') THEN
        ALTER TABLE \"AspNetUsers\"
          ADD CONSTRAINT \"FK_AspNetUsers_Tenants_TenantId\"
          FOREIGN KEY (\"TenantId\") REFERENCES \"Tenants\" (\"Id\") ON DELETE RESTRICT;
    END IF;
  END IF;
END
$$;";

            migrationBuilder.Sql(addFkSql);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove FK if exists
            var dropFkSql = @"
DO $$
BEGIN
  IF EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_AspNetUsers_Tenants_TenantId') THEN
    ALTER TABLE \"AspNetUsers\" DROP CONSTRAINT \"FK_AspNetUsers_Tenants_TenantId\";
  END IF;
END
$$;";
            migrationBuilder.Sql(dropFkSql);

            // Drop indexes if they exist
            migrationBuilder.DropIndex(
                name: "IX_AspNetUsers_TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropIndex(
                name: "IX_AspNetRoles_TenantId",
                table: "AspNetRoles");

            // Drop columns
            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AspNetRoles");
        }
    }
}
