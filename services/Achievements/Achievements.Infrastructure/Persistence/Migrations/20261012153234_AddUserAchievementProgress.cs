using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Achievements.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserAchievementProgress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProcessedFacts",
                columns: table => new
                {
                    EventId = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    AcceptedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProcessedFacts", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "UserAchievementStates",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PostsObserved = table.Column<int>(type: "integer", nullable: false),
                    FollowersObserved = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAchievementStates", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "EarnedAchievements",
                columns: table => new
                {
                    AchievementId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    EarnedOnUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EarnedAchievements", x => new { x.UserId, x.AchievementId });
                    table.ForeignKey(
                        name: "FK_EarnedAchievements_UserAchievementStates_UserId",
                        column: x => x.UserId,
                        principalTable: "UserAchievementStates",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EarnedAchievements");

            migrationBuilder.DropTable(
                name: "ProcessedFacts");

            migrationBuilder.DropTable(
                name: "UserAchievementStates");
        }
    }
}
