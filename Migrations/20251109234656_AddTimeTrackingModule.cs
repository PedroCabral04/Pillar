using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class AddTimeTrackingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TimeSheets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    Month = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ReviewedById = table.Column<int>(type: "integer", nullable: true),
                    ReviewComments = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TotalHoursWorked = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    TotalOvertimeHours = table.Column<decimal>(type: "numeric(8,2)", nullable: false),
                    TotalAbsenceDays = table.Column<int>(type: "integer", nullable: false),
                    TotalJustifiedAbsenceDays = table.Column<int>(type: "integer", nullable: false),
                    TotalWorkedDays = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSheets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeSheets_AspNetUsers_ReviewedById",
                        column: x => x.ReviewedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TimeSheets_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TimeSheetEntries",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TimeSheetId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EntryType = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    HoursWorked = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "numeric(5,2)", nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    AttachmentPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TimeSheetEntries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TimeSheetEntries_TimeSheets_TimeSheetId",
                        column: x => x.TimeSheetId,
                        principalTable: "TimeSheets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_timesheet_entry_date",
                table: "TimeSheetEntries",
                columns: new[] { "TimeSheetId", "Date" });

            migrationBuilder.CreateIndex(
                name: "IX_TimeSheetEntries_EntryType",
                table: "TimeSheetEntries",
                column: "EntryType");

            migrationBuilder.CreateIndex(
                name: "idx_timesheet_user_period",
                table: "TimeSheets",
                columns: new[] { "UserId", "Year", "Month" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TimeSheets_CreatedAt",
                table: "TimeSheets",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSheets_ReviewedById",
                table: "TimeSheets",
                column: "ReviewedById");

            migrationBuilder.CreateIndex(
                name: "IX_TimeSheets_Status",
                table: "TimeSheets",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TimeSheetEntries");

            migrationBuilder.DropTable(
                name: "TimeSheets");
        }
    }
}
