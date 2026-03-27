using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptionGen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUsage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "user_usage",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    period_start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    captions_used = table.Column<int>(type: "integer", nullable: false),
                    media_used = table.Column<int>(type: "integer", nullable: false),
                    updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_usage", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_usage_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_usage_user_id_period_start_utc",
                table: "user_usage",
                columns: new[] { "user_id", "period_start_utc" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_usage");
        }
    }
}

