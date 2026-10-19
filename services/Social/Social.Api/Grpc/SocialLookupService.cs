using Grpc.Core;
using MediatR;
using Venture.Application.Social.Queries.GetFollowStats;
using Venture.Application.Social.Queries.GetFollowing;
using Venture.Contracts.Social;
using Venture.Domain.Common.Results;

namespace Venture.Api.Grpc;

public sealed class SocialLookupService(ISender sender) : SocialLookup.SocialLookupBase
{
    public override async Task<FollowingReply> GetFollowing(UserIdRequest request, ServerCallContext context)
    {
        var userId = ParseId(request.Id);
        var result = await sender.Send(new GetFollowingIdsQuery(userId), context.CancellationToken);
        if (result.IsFailure)
            throw Failure(result);

        var reply = new FollowingReply();
        reply.Ids.AddRange(result.Value.Select(id => id.ToString("D")));
        return reply;
    }

    public override async Task<FollowStatsReply> GetFollowStats(UserIdRequest request, ServerCallContext context)
    {
        var userId = ParseId(request.Id);
        var result = await sender.Send(new GetFollowStatsQuery(userId), context.CancellationToken);
        if (result.IsFailure)
            throw Failure(result);

        return new FollowStatsReply
        {
            Followers = result.Value.Followers,
            Following = result.Value.Following
        };
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
