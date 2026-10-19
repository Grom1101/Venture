using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Caching;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Achievements;
using Venture.Application.Caching;
using Venture.Domain.Common.Results;
using Venture.Application.Social;
using Venture.Application.Users.Queries.GetUserProfile;

namespace Venture.Application.Users.Queries.GetMyProfile;

internal sealed class GetMyProfileQueryHandler(
    IReadDbContext readDbContext,
    ICurrentUser currentUser,
    IFollowStats followStats,
    IUserAchievementDisplay achievementDisplay,
    ICacheService cache)
    : IQueryHandler<GetMyProfileQuery, UserProfileDto>
{
    public async Task<Result<UserProfileDto>> Handle(
        GetMyProfileQuery query,
        CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } currentUserId)
            return Result.Failure<UserProfileDto>(UserErrors.NotAuthenticated);

        var cacheKey = CacheKeys.UserProfileById(currentUserId.Value);
        var profile = await cache.GetAsync<UserProfileDto>(cacheKey, cancellationToken);

        if (profile is null)
        {
            profile = await UserProfileReader.ReadAsync(
                readDbContext.Users.Where(user => user.Id == currentUserId),
                followStats,
                cancellationToken);

            if (profile is null)
                return Result.Failure<UserProfileDto>(UserErrors.NotFound(currentUserId.Value));

            await cache.SetAsync(cacheKey, profile, CacheDurations.UserProfile, cancellationToken);
            await cache.SetAsync(
                CacheKeys.UserProfileByUsername(profile.Username),
                profile,
                CacheDurations.UserProfile,
                cancellationToken);
        }

        var achievements = await achievementDisplay.GetAsync(profile.Id, cancellationToken);
        return Result.Success(profile with { Achievements = achievements });
    }
}
