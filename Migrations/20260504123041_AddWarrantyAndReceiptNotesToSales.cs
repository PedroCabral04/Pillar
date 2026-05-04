using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class AddWarrantyAndReceiptNotesToSales : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReceiptNotes",
                table: "Sales",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "WarrantyExpiration",
                table: "Sales",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WarrantyType",
                table: "Sales",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReceiptNotes",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "WarrantyExpiration",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "WarrantyType",
                table: "Sales");
        }
    }
}
