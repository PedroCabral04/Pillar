using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class BackfillServiceOrderActualCompletionDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrderAttachments_AspNetUsers_UploadedByUserId",
                table: "ServiceOrderAttachments");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrderAttachments_AspNetUsers_UploadedByUserId",
                table: "ServiceOrderAttachments",
                column: "UploadedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.Sql(
                @"UPDATE ""ServiceOrders""
                  SET ""ActualCompletionDate"" = COALESCE(""UpdatedAt"", ""EntryDate"")
                  WHERE ""Status"" IN ('Completed', 'Delivered')
                    AND ""ActualCompletionDate"" IS NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceOrderAttachments_AspNetUsers_UploadedByUserId",
                table: "ServiceOrderAttachments");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceOrderAttachments_AspNetUsers_UploadedByUserId",
                table: "ServiceOrderAttachments",
                column: "UploadedByUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
