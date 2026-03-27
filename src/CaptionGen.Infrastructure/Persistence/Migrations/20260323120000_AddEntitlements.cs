using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CaptionGen.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEntitlements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "plans",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    slug = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    caption_generations_per_month = table.Column<int>(type: "integer", nullable: false),
                    media_assets_limit = table.Column<int>(type: "integer", nullable: false),
                    seats_included = table.Column<int>(type: "integer", nullable: false),
                    scheduling_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    ai_improve_enabled = table.Column<bool>(type: "boolean", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_plans", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "user_entitlements",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                    active_until_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    seats_in_use = table.Column<int>(type: "integer", nullable: false),
                    created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_entitlements", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_entitlements_plans_plan_id",
                        column: x => x.plan_id,
                        principalTable: "plans",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_entitlements_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_plans_slug",
                table: "plans",
                column: "slug",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_entitlements_plan_id",
                table: "user_entitlements",
                column: "plan_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_entitlements_user_id",
                table: "user_entitlements",
                column: "user_id",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "user_entitlements");

            migrationBuilder.DropTable(
                name: "plans");
        }
    }
}

