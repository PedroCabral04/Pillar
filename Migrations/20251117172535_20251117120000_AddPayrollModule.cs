using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class _20251117120000_AddPayrollModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ApprovedAt",
                table: "PayrollPeriods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ApprovedById",
                table: "PayrollPeriods",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CalculationDate",
                table: "PayrollPeriods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "PayrollPeriods",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PaidAt",
                table: "PayrollPeriods",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PaidById",
                table: "PayrollPeriods",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEmployerCost",
                table: "PayrollPeriods",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalGrossAmount",
                table: "PayrollPeriods",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalInssAmount",
                table: "PayrollPeriods",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIrrfAmount",
                table: "PayrollPeriods",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalNetAmount",
                table: "PayrollPeriods",
                type: "numeric(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "PayrollResults",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PayrollPeriodId = table.Column<int>(type: "integer", nullable: false),
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    PayrollEntryId = table.Column<int>(type: "integer", nullable: true),
                    EmployeeNameSnapshot = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmployeeCpfSnapshot = table.Column<string>(type: "character varying(14)", maxLength: 14, nullable: true),
                    DepartmentSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    PositionSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BankNameSnapshot = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    BankAgencySnapshot = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    BankAccountSnapshot = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    DependentsSnapshot = table.Column<int>(type: "integer", nullable: false),
                    BaseSalarySnapshot = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalEarnings = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalDeductions = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalContributions = table.Column<decimal>(type: "numeric", nullable: false),
                    NetAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    GrossAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    InssAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    IrrfAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    AdditionalEmployerCost = table.Column<decimal>(type: "numeric", nullable: true),
                    PaymentDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollResults_AspNetUsers_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollResults_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollResults_PayrollEntries_PayrollEntryId",
                        column: x => x.PayrollEntryId,
                        principalTable: "PayrollEntries",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollResults_PayrollPeriods_PayrollPeriodId",
                        column: x => x.PayrollPeriodId,
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollTaxBrackets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TaxType = table.Column<int>(type: "integer", nullable: false),
                    RangeStart = table.Column<decimal>(type: "numeric", nullable: false),
                    RangeEnd = table.Column<decimal>(type: "numeric", nullable: true),
                    Rate = table.Column<decimal>(type: "numeric", nullable: false),
                    Deduction = table.Column<decimal>(type: "numeric", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    EffectiveTo = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollTaxBrackets", x => x.Id);
                });

            var effectiveFrom = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            migrationBuilder.InsertData(
                table: "PayrollTaxBrackets",
                columns: new[] { "Id", "TaxType", "RangeStart", "RangeEnd", "Rate", "Deduction", "IsActive", "EffectiveFrom", "EffectiveTo", "SortOrder" },
                values: new object[,]
                {
                    { 1, 0, 0m, 1412.00m, 0.075m, 0m, true, effectiveFrom, null, 1 },
                    { 2, 0, 1412.01m, 2666.68m, 0.09m, 0m, true, effectiveFrom, null, 2 },
                    { 3, 0, 2666.69m, 4000.03m, 0.12m, 0m, true, effectiveFrom, null, 3 },
                    { 4, 0, 4000.04m, 7786.02m, 0.14m, 0m, true, effectiveFrom, null, 4 },
                    { 5, 1, 0m, 2259.20m, 0m, 0m, true, effectiveFrom, null, 1 },
                    { 6, 1, 2259.21m, 2826.65m, 0.075m, 169.44m, true, effectiveFrom, null, 2 },
                    { 7, 1, 2826.66m, 3751.05m, 0.15m, 381.44m, true, effectiveFrom, null, 3 },
                    { 8, 1, 3751.06m, 4664.68m, 0.225m, 662.77m, true, effectiveFrom, null, 4 },
                    { 9, 1, 4664.69m, null, 0.275m, 896m, true, effectiveFrom, null, 5 }
                });

            migrationBuilder.CreateTable(
                name: "PayrollComponents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PayrollResultId = table.Column<int>(type: "integer", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Code = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    Description = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    BaseAmount = table.Column<decimal>(type: "numeric", nullable: true),
                    ReferenceQuantity = table.Column<decimal>(type: "numeric", nullable: true),
                    ImpactsFgts = table.Column<bool>(type: "boolean", nullable: false),
                    IsTaxable = table.Column<bool>(type: "boolean", nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollComponents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollComponents_PayrollResults_PayrollResultId",
                        column: x => x.PayrollResultId,
                        principalTable: "PayrollResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayrollSlips",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PayrollResultId = table.Column<int>(type: "integer", nullable: false),
                    FilePath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FileHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FileSize = table.Column<long>(type: "bigint", nullable: false),
                    GeneratedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    GeneratedById = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedById = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollSlips", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollSlips_AspNetUsers_GeneratedById",
                        column: x => x.GeneratedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PayrollSlips_AspNetUsers_UpdatedById",
                        column: x => x.UpdatedById,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PayrollSlips_PayrollResults_PayrollResultId",
                        column: x => x.PayrollResultId,
                        principalTable: "PayrollResults",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_ApprovedById",
                table: "PayrollPeriods",
                column: "ApprovedById");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollPeriods_PaidById",
                table: "PayrollPeriods",
                column: "PaidById");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollComponents_PayrollResultId",
                table: "PayrollComponents",
                column: "PayrollResultId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResults_EmployeeId",
                table: "PayrollResults",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResults_PayrollEntryId",
                table: "PayrollResults",
                column: "PayrollEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResults_PayrollPeriodId",
                table: "PayrollResults",
                column: "PayrollPeriodId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollResults_UpdatedById",
                table: "PayrollResults",
                column: "UpdatedById");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSlips_GeneratedById",
                table: "PayrollSlips",
                column: "GeneratedById");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSlips_PayrollResultId",
                table: "PayrollSlips",
                column: "PayrollResultId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PayrollSlips_UpdatedById",
                table: "PayrollSlips",
                column: "UpdatedById");

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_ApprovedById",
                table: "PayrollPeriods",
                column: "ApprovedById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_PaidById",
                table: "PayrollPeriods",
                column: "PaidById",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_ApprovedById",
                table: "PayrollPeriods");

            migrationBuilder.DropForeignKey(
                name: "FK_PayrollPeriods_AspNetUsers_PaidById",
                table: "PayrollPeriods");

            migrationBuilder.DropTable(
                name: "PayrollComponents");

            migrationBuilder.DropTable(
                name: "PayrollSlips");

            migrationBuilder.DropTable(
                name: "PayrollTaxBrackets");

            migrationBuilder.DropTable(
                name: "PayrollResults");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_ApprovedById",
                table: "PayrollPeriods");

            migrationBuilder.DropIndex(
                name: "IX_PayrollPeriods_PaidById",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "ApprovedAt",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "ApprovedById",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "CalculationDate",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "PaidAt",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "PaidById",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "TotalEmployerCost",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "TotalGrossAmount",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "TotalInssAmount",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "TotalIrrfAmount",
                table: "PayrollPeriods");

            migrationBuilder.DropColumn(
                name: "TotalNetAmount",
                table: "PayrollPeriods");
        }
    }
}
