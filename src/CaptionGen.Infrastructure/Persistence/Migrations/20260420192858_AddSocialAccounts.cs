using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptionGen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSocialAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "social_accounts",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    platform = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    platform_user_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    display_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    access_token_encrypted = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: false),
                    refresh_token_encrypted = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    token_expires_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    connected_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_social_accounts", x => x.id);
                    table.ForeignKey(
                        name: "FK_social_accounts_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_social_accounts_user_id_platform",
                table: "social_accounts",
                columns: new[] { "user_id", "platform" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "social_accounts");
        }
    }
}
