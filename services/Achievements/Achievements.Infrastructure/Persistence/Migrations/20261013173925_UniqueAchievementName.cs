using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Achievements.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UniqueAchievementName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Achievements_AchievementType",
                table: "Achievements");

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_AchievementType",
                table: "Achievements",
                column: "AchievementType");

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_Name",
                table: "Achievements",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Achievements_AchievementType",
                table: "Achievements");

            migrationBuilder.DropIndex(
                name: "IX_Achievements_Name",
                table: "Achievements");

            migrationBuilder.CreateIndex(
                name: "IX_Achievements_AchievementType",
                table: "Achievements",
                column: "AchievementType",
                unique: true);
        }
    }
}
