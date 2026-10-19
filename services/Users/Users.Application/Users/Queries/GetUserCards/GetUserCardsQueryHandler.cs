using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Domain.Common.Results;
using Venture.Application.Users.Queries.GetUserCard;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Users.Queries.GetUserCards;

public sealed record GetUserCardsQuery(IReadOnlyCollection<Guid> Ids) : IQuery<IReadOnlyCollection<UserCardDto>>;

internal sealed class GetUserCardsQueryHandler(IReadDbContext readDbContext)
    : IQueryHandler<GetUserCardsQuery, IReadOnlyCollection<UserCardDto>>
{
    public async Task<Result<IReadOnlyCollection<UserCardDto>>> Handle(
        GetUserCardsQuery query,
        CancellationToken cancellationToken)
    {
        if (query.Ids.Count == 0)
            return Result.Success<IReadOnlyCollection<UserCardDto>>([]);

        var parsedIds = query.Ids.Select(UserId.From).ToArray();
        var invalidId = parsedIds.FirstOrDefault(id => id.IsFailure);
        if (invalidId is not null)
            return Result.Failure<IReadOnlyCollection<UserCardDto>>(invalidId.Error);

        var ids = parsedIds.Select(id => id.Value).ToArray();

        var cards = await readDbContext.Users
            .Where(user => ids.Contains(user.Id))
            .Select(user => new UserCardDto(user.Id.Value, user.Username.Value, user.DisplayName.Value))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyCollection<UserCardDto>>(cards);
    }
}
