using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class AddChatbotAuditEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ChatbotAuditEntries",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: true),
                    TenantId = table.Column<int>(type: "integer", nullable: true),
                    ConversationId = table.Column<int>(type: "integer", nullable: true),
                    Source = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Outcome = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    RequestMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    EffectiveMessage = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: false),
                    ResponseMessage = table.Column<string>(type: "character varying(8000)", maxLength: 8000, nullable: true),
                    Error = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    OperationMode = table.Column<int>(type: "integer", nullable: false),
                    ResponseStyle = table.Column<int>(type: "integer", nullable: false),
                    IsConfirmedAction = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresConfirmation = table.Column<bool>(type: "boolean", nullable: false),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    SuggestedActionsJson = table.Column<string>(type: "text", nullable: true),
                    EvidenceSourcesJson = table.Column<string>(type: "text", nullable: true),
                    AiProvider = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    AiConfigured = table.Column<bool>(type: "boolean", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChatbotAuditEntries", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotAuditEntries_ConversationId",
                table: "ChatbotAuditEntries",
                column: "ConversationId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotAuditEntries_CreatedAt",
                table: "ChatbotAuditEntries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotAuditEntries_TenantId",
                table: "ChatbotAuditEntries",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotAuditEntries_UserId",
                table: "ChatbotAuditEntries",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ChatbotAuditEntries_UserId_CreatedAt",
                table: "ChatbotAuditEntries",
                columns: new[] { "UserId", "CreatedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ChatbotAuditEntries");
        }
    }
}
