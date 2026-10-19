using Venture.Application.Users.Queries.GetUserProfile;

namespace Venture.Application.Achievements;

public interface IUserAchievementDisplay
{
    Task<IReadOnlyCollection<UserAchievementDto>> GetAsync(Guid userId, CancellationToken cancellationToken);
}
