using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class RefineAuditSystem_AddTenantAndReferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "EntityDescription",
                table: "AuditLogs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "References",
                table: "AuditLogs",
                type: "jsonb",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AuditLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "idx_audit_tenant_entity_timeline",
                table: "AuditLogs",
                columns: new[] { "TenantId", "EntityName", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "idx_audit_tenant_timeline",
                table: "AuditLogs",
                columns: new[] { "TenantId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "idx_audit_tenant_entity_timeline",
                table: "AuditLogs");

            migrationBuilder.DropIndex(
                name: "idx_audit_tenant_timeline",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "EntityDescription",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "References",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AuditLogs");
        }
    }
}
