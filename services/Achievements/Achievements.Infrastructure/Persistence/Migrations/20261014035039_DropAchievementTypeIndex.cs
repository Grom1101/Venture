using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Achievements.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class DropAchievementTypeIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Achievements_AchievementType",
                table: "Achievements");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Achievements_AchievementType",
                table: "Achievements",
                column: "AchievementType");
        }
    }
}
