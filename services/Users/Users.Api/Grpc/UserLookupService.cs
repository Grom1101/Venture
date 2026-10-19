using Grpc.Core;
using MediatR;
using Venture.Application.Users.Queries.GetUserCard;
using Venture.Application.Users.Queries.GetUserCards;
using Venture.Contracts.Users;
using Venture.Domain.Common.Results;

namespace Venture.Api.Grpc;

public sealed class UserLookupService(ISender sender) : UserLookup.UserLookupBase
{
    public override async Task<ExistsReply> Exists(UserIdRequest request, ServerCallContext context)
    {
        var userId = ParseId(request.Id);
        var result = await sender.Send(new GetUserCardQuery(userId), context.CancellationToken);

        if (result.IsSuccess)
            return new ExistsReply { Exists = true };

        if (result.Error.Type == ErrorType.NotFound)
            return new ExistsReply { Exists = false };

        throw Failure(result);
    }

    public override async Task<GetCardsReply> GetCards(GetCardsRequest request, ServerCallContext context)
    {
        var ids = request.Ids.Select(ParseId).ToArray();
        var result = await sender.Send(new GetUserCardsQuery(ids), context.CancellationToken);
        if (result.IsFailure)
            throw Failure(result);

        var reply = new GetCardsReply();
        reply.Cards.AddRange(result.Value.Select(card => new UserCard
        {
            Id = card.Id.ToString("D"),
            Username = card.Username,
            DisplayName = card.DisplayName
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
