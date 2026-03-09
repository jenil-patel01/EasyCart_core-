using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace eays.Migrations
{
    /// <inheritdoc />
    public partial class FixCartItemsForeignKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop the orphaned FK constraint if it still exists in the database
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.foreign_keys
                    WHERE name = 'FK_CartItems_AspNetUsers_UserId'
                )
                BEGIN
                    ALTER TABLE [CartItems] DROP CONSTRAINT [FK_CartItems_AspNetUsers_UserId];
                END
            ");

            // Drop the associated index if it still exists
            migrationBuilder.Sql(@"
                IF EXISTS (
                    SELECT 1 FROM sys.indexes
                    WHERE name = 'IX_CartItems_UserId' AND object_id = OBJECT_ID('CartItems')
                )
                BEGIN
                    DROP INDEX [IX_CartItems_UserId] ON [CartItems];
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
