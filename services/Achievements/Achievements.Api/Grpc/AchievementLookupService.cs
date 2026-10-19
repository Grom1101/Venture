using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Venture.Application.Achievements.Queries.GetUserAchievements;
using Venture.Contracts.Achievements;
using Venture.Domain.Common.Results;

namespace Venture.Api.Grpc;

public sealed class AchievementLookupService(ISender sender) : AchievementLookup.AchievementLookupBase
{
    public override async Task<AchievementsReply> GetUserAchievements(
        UserIdRequest request,
        ServerCallContext context)
    {
        var userId = ParseId(request.Id);
        var result = await sender.Send(new GetUserAchievementsQuery(userId), context.CancellationToken);
        if (result.IsFailure)
            throw Failure(result);

        var reply = new AchievementsReply();
        reply.Achievements.AddRange(result.Value.Select(achievement => new UserAchievement
        {
            Id = achievement.Id.ToString("D"),
            Type = achievement.Type,
            Name = achievement.Name,
            Description = achievement.Description,
            EarnedOnUtc = Timestamp.FromDateTime(DateTime.SpecifyKind(achievement.EarnedOnUtc, DateTimeKind.Utc))
        }));

        return reply;
    }

    private static Guid ParseId(string value)
    {
        if (Guid.TryParse(value, out var id))
            return id;

        throw new RpcException(new Status(StatusCode.InvalidArgument, "Некорректный идентификатор пользователя."));
    }

    private static RpcException Failure(Result result)
    {
        var status = result.Error.Type switch
        {
            ErrorType.NotFound => StatusCode.NotFound,
            ErrorType.Conflict => StatusCode.AlreadyExists,
            _ => StatusCode.InvalidArgument
        };

        return new RpcException(new Status(status, result.Error.Description));
    }
}
