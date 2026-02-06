using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleActionPermissions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ModuleActionPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ModulePermissionId = table.Column<int>(type: "integer", nullable: false),
                    ActionKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ModuleActionPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ModuleActionPermissions_ModulePermissions_ModulePermissionId",
                        column: x => x.ModulePermissionId,
                        principalTable: "ModulePermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RoleModuleActionPermissions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    ModuleActionPermissionId = table.Column<int>(type: "integer", nullable: false),
                    GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GrantedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RoleModuleActionPermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RoleModuleActionPermissions_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RoleModuleActionPermissions_AspNetUsers_GrantedByUserId",
                        column: x => x.GrantedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_RoleModuleActionPermissions_ModuleActionPermissions_ModuleA~",
                        column: x => x.ModuleActionPermissionId,
                        principalTable: "ModuleActionPermissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ModuleActionPermissions_ModulePermissionId_ActionKey",
                table: "ModuleActionPermissions",
                columns: new[] { "ModulePermissionId", "ActionKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ModuleActionPermissions_ModulePermissionId_DisplayOrder",
                table: "ModuleActionPermissions",
                columns: new[] { "ModulePermissionId", "DisplayOrder" });

            migrationBuilder.CreateIndex(
                name: "IX_RoleModuleActionPermissions_GrantedByUserId",
                table: "RoleModuleActionPermissions",
                column: "GrantedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleModuleActionPermissions_ModuleActionPermissionId",
                table: "RoleModuleActionPermissions",
                column: "ModuleActionPermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RoleModuleActionPermissions_RoleId_ModuleActionPermissionId",
                table: "RoleModuleActionPermissions",
                columns: new[] { "RoleId", "ModuleActionPermissionId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RoleModuleActionPermissions");

            migrationBuilder.DropTable(
                name: "ModuleActionPermissions");
        }
    }
}
