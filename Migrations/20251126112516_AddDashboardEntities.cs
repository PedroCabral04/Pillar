using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserDashboardLayouts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    LayoutJson = table.Column<string>(type: "jsonb", nullable: false),
                    LayoutType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Columns = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDashboardLayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDashboardLayouts_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "WidgetRoleConfigurations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ProviderKey = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    WidgetKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    RolesJson = table.Column<string>(type: "jsonb", nullable: true),
                    LastModified = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedByUserId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WidgetRoleConfigurations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WidgetRoleConfigurations_AspNetUsers_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserDashboardLayouts_UserId",
                table: "UserDashboardLayouts",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WidgetRoleConfigurations_ModifiedByUserId",
                table: "WidgetRoleConfigurations",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WidgetRoleConfigurations_ProviderKey_WidgetKey",
                table: "WidgetRoleConfigurations",
                columns: new[] { "ProviderKey", "WidgetKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserDashboardLayouts");

            migrationBuilder.DropTable(
                name: "WidgetRoleConfigurations");
        }
    }
}
