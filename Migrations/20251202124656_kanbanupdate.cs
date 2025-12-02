using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace erp.Migrations
{
    /// <inheritdoc />
    public partial class kanbanupdate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add CreatedAt column to KanbanBoards if it doesn't exist
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'KanbanBoards' AND column_name = 'CreatedAt'
                    ) THEN
                        ALTER TABLE ""KanbanBoards"" 
                        ADD COLUMN ""CreatedAt"" timestamp with time zone NOT NULL DEFAULT NOW();
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "KanbanBoards");
        }
    }
}
