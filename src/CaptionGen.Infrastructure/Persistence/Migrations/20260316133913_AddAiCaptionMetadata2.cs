using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptionGen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiCaptionMetadata2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // No-op migration kept to preserve the migration history already applied in existing environments.
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No schema changes were applied by Up, so there is nothing to roll back.
        }
    }
}
