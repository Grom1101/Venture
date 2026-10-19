using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Users.Queries.GetUserCard;

public sealed record UserCardDto(Guid Id, string Username, string DisplayName);

public sealed record GetUserCardQuery(Guid UserId) : IQuery<UserCardDto>;

internal sealed class GetUserCardQueryHandler(IReadDbContext readDbContext)
    : IQueryHandler<GetUserCardQuery, UserCardDto>
{
    public async Task<Result<UserCardDto>> Handle(GetUserCardQuery query, CancellationToken cancellationToken)
    {
        var userId = UserId.From(query.UserId);
        if (userId.IsFailure)
            return Result.Failure<UserCardDto>(userId.Error);

        var card = await readDbContext.Users
            .Where(user => user.Id == userId.Value)
            .Select(user => new UserCardDto(user.Id.Value, user.Username.Value, user.DisplayName.Value))
            .FirstOrDefaultAsync(cancellationToken);

        return card is null
            ? Result.Failure<UserCardDto>(UserErrors.NotFound(query.UserId))
            : Result.Success(card);
    }
}
