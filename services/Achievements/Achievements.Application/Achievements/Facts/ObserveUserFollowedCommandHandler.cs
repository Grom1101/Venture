using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Time;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Achievements.Facts;

internal sealed class ObserveUserFollowedCommandHandler(
    IAchievementRepository achievements,
    IAchievementProgressStore progress,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ObserveUserFollowedCommand>
{
    public async Task<Result> Handle(ObserveUserFollowedCommand command, CancellationToken cancellationToken)
    {
        if (command.SchemaVersion != 1)
            throw new UnsupportedFactSchemaException(command.SchemaVersion, command.EventId);

        var catalog = await achievements.GetByNameAsync(CatalogAchievements.FirstFollowerName, cancellationToken)
            ?? throw new InvalidOperationException($"В каталоге нет достижения {CatalogAchievements.FirstFollowerName}.");

        if (!await progress.TryAcceptAsync(command.EventId, dateTimeProvider.UtcNow, cancellationToken))
            return Result.Success();

        var followingId = UserId.From(command.FollowingId);
        if (followingId.IsFailure)
            return Result.Failure(followingId.Error);

        var state = await progress.LockAsync(followingId.Value, cancellationToken);
        var observed = state.ObserveFollower(catalog.Id, dateTimeProvider.UtcNow);
        if (observed.IsFailure)
            return observed;

        return Result.Success();
    }
}
