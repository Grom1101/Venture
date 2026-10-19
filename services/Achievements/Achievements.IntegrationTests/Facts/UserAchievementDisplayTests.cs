using System.Text.Json;
using FluentAssertions;
using Venture.Application.Achievements.Queries.GetAchievementGroups;
using Venture.Application.Achievements.Queries.GetUserAchievements;
using Venture.Domain.Achievements;
using Venture.Domain.Achievements.Enums;
using Venture.Domain.Users.ValueObjects;

namespace Venture.IntegrationTests.Facts;

public class UserAchievementDisplayTests
{
    [Fact]
    public async Task GetUserAchievements_ReturnsEmptyArray_WhenNothingWasEarned()
    {
        await using var database = await ProgressDatabase.CreateAsync();

        var result = await new GetUserAchievementsQueryHandler(database.Db)
            .Handle(new GetUserAchievementsQuery(Guid.NewGuid()), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task CatalogItem_SerializesEarnedOnUtc_AsNullWhenItWasNotEarned()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        var plain = new AchievementCatalogItemDto(
            Guid.Parse("2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11"),
            "Первый пост",
            "Опубликован первый пост",
            null);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(plain, options));
        var names = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();

        names.Should().Equal("id", "name", "description", "earnedOnUtc");
        document.RootElement.GetProperty("earnedOnUtc").ValueKind.Should().Be(JsonValueKind.Null);
    }

    [Fact]
    public async Task DisplayDto_SerializesTheProfileFields()
    {
        var dto = new UserAchievementDisplayDto(
            Guid.Parse("2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11"),
            "FirstPost",
            "Первый пост",
            "Опубликован первый пост",
            new DateTime(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc));

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(dto, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        var names = document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();

        names.Should().Equal("id", "type", "name", "description", "earnedOnUtc");
    }
}
