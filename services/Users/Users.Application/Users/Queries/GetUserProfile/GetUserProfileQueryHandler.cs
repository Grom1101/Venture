using Venture.Application.Abstractions.Caching;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Achievements;
using Venture.Application.Caching;
using Venture.Domain.Common.Results;
using Venture.Application.Social;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Users.Queries.GetUserProfile;

internal sealed class GetUserProfileQueryHandler(
    IReadDbContext readDbContext,
    IFollowStats followStats,
    IUserAchievementDisplay achievementDisplay,
    ICacheService cache)
    : IQueryHandler<GetUserProfileQuery, UserProfileDto>
{
    public async Task<Result<UserProfileDto>> Handle(
        GetUserProfileQuery query,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.UserProfileByUsername(query.Username);
        var profile = await cache.GetAsync<UserProfileDto>(cacheKey, cancellationToken);

        if (profile is null)
        {
            var username = Username.Create(query.Username);
            if (username.IsFailure)
                return Result.Failure<UserProfileDto>(username.Error);

            profile = await UserProfileReader.ReadAsync(
                readDbContext.Users.Where(user => user.Username == username.Value),
                followStats,
                cancellationToken);

            if (profile is null)
                return Result.Failure<UserProfileDto>(UserErrors.NotFoundByUsername(query.Username));

            await cache.SetAsync(cacheKey, profile, CacheDurations.UserProfile, cancellationToken);
            await cache.SetAsync(
                CacheKeys.UserProfileById(profile.Id),
                profile,
                CacheDurations.UserProfile,
                cancellationToken);
        }

        var achievements = await achievementDisplay.GetAsync(profile.Id, cancellationToken);
        return Result.Success(profile with { Achievements = achievements });
    }
}
