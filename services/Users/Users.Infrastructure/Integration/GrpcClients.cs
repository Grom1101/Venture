using Grpc.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Venture.Application.Achievements;
using Venture.Application.Social;
using Venture.Application.Users.Queries.GetUserProfile;
using Venture.Domain.Users.ValueObjects;
using Achievements = Venture.Contracts.Achievements;
using Social = Venture.Contracts.Social;

namespace Venture.Infrastructure.Integration;

internal static class AchievementDisplayClient
{
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(2);
}

internal static class GrpcClientRegistration
{
    public static IServiceCollection AddSocialGrpcClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGrpcClient<Social.SocialLookup.SocialLookupClient>(options =>
            options.Address = ServiceAddress(configuration, "Social"));

        return services;
    }

    public static IServiceCollection AddAchievementsGrpcClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddGrpcClient<Achievements.AchievementLookup.AchievementLookupClient>(options =>
            options.Address = ServiceAddress(configuration, "Achievements"));

        return services;
    }

    private static Uri ServiceAddress(IConfiguration configuration, string name)
    {
        var address = configuration[$"Grpc:{name}"]
            ?? throw new InvalidOperationException($"Grpc:{name} is not configured.");

        return new Uri(address);
    }
}

internal sealed class GrpcFollowStats(Social.SocialLookup.SocialLookupClient client) : IFollowStats
{
    public async Task<FollowStats> GetAsync(UserId userId, CancellationToken cancellationToken)
    {
        var reply = await client.GetFollowStatsAsync(
            new Social.UserIdRequest { Id = userId.Value.ToString("D") },
            cancellationToken: cancellationToken);

        return new FollowStats(reply.Followers, reply.Following);
    }
}

internal sealed class GrpcUserAchievementDisplay(
    Achievements.AchievementLookup.AchievementLookupClient client,
    ILogger<GrpcUserAchievementDisplay> logger) : IUserAchievementDisplay
{
    public async Task<IReadOnlyCollection<UserAchievementDto>> GetAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var reply = await client.GetUserAchievementsAsync(
                new Achievements.UserIdRequest { Id = userId.ToString("D") },
                deadline: DateTime.UtcNow.Add(AchievementDisplayClient.Timeout),
                cancellationToken: cancellationToken);

            return reply.Achievements.Select(Read).ToArray();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                "Список достижений пользователя {UserId} не получен: таймаут Achievements",
                userId);
            return [];
        }
        catch (Exception exception) when (!cancellationToken.IsCancellationRequested
                                           && exception is RpcException or InvalidOperationException)
        {
            logger.LogWarning(
                exception,
                "Список достижений пользователя {UserId} не получен",
                userId);
            return [];
        }
    }

    private static UserAchievementDto Read(Achievements.UserAchievement achievement)
    {
        if (!Guid.TryParse(achievement.Id, out var id) || achievement.EarnedOnUtc is null)
            throw new InvalidOperationException("В ответе Achievements нет идентификатора или даты выдачи.");

        return new UserAchievementDto(
            id,
            achievement.Type,
            achievement.Name,
            achievement.Description,
            achievement.EarnedOnUtc.ToDateTime());
    }
}
