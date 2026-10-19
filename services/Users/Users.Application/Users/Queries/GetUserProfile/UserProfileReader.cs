using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Social;
using Venture.Domain.Users;

namespace Venture.Application.Users.Queries.GetUserProfile;

/// <summary>
/// Общая проекция профиля для «моего» и «чужого» профиля: агрегат не загружается,
/// из БД вытаскиваются только колонки, которые нужны UI.
/// Счётчики подписок берутся из Social. Достижения в эту проекцию не входят:
/// их читают отдельно и не кладут в кэш профиля.
/// </summary>
internal static class UserProfileReader
{
    public static async Task<UserProfileDto?> ReadAsync(
        IQueryable<User> source,
        IFollowStats followStats,
        CancellationToken cancellationToken)
    {
        var user = await source
            .Select(user => new
            {
                user.Id,
                user.Username,
                user.DisplayName,
                user.Bio,
                user.CreatedAtUtc,
                Skills = user.Skills.ToList()
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
            return null;

        var stats = await followStats.GetAsync(user.Id, cancellationToken);

        return new UserProfileDto(
            user.Id.Value,
            user.Username.Value,
            user.DisplayName.Value,
            new UserBioDto(
                user.Bio.About,
                user.Bio.FocusedOn,
                user.Bio.Comment,
                user.Bio.Interests.Select(interest => interest.Value).ToArray()),
            stats.Followers,
            stats.Following,
            user.Skills
                .Select(skill => new UserSkillDto(skill.Name.Value, skill.Level.ToString()))
                .ToArray(),
            [],
            user.CreatedAtUtc);
    }
}
