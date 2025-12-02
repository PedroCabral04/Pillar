using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class AddUserOnboardingProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserOnboardingProgress_UserId",
                table: "UserOnboardingProgress");

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingProgress_UserId_TourId",
                table: "UserOnboardingProgress",
                columns: new[] { "UserId", "TourId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserOnboardingProgress_UserId_TourId",
                table: "UserOnboardingProgress");

            migrationBuilder.CreateIndex(
                name: "IX_UserOnboardingProgress_UserId",
                table: "UserOnboardingProgress",
                column: "UserId");
        }
    }
}
