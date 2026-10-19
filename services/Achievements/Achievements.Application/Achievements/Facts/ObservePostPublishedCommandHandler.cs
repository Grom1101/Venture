using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Time;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Achievements.Facts;

internal sealed class ObservePostPublishedCommandHandler(
    IAchievementRepository achievements,
    IAchievementProgressStore progress,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<ObservePostPublishedCommand>
{
    public async Task<Result> Handle(ObservePostPublishedCommand command, CancellationToken cancellationToken)
    {
        if (command.SchemaVersion != 1)
            throw new UnsupportedFactSchemaException(command.SchemaVersion, command.EventId);

        var catalog = await achievements.GetByNameAsync(CatalogAchievements.FirstPostName, cancellationToken)
            ?? throw new InvalidOperationException($"В каталоге нет достижения {CatalogAchievements.FirstPostName}.");

        if (!await progress.TryAcceptAsync(command.EventId, dateTimeProvider.UtcNow, cancellationToken))
            return Result.Success();

        var authorId = UserId.From(command.AuthorId);
        if (authorId.IsFailure)
            return Result.Failure(authorId.Error);

        var state = await progress.LockAsync(authorId.Value, cancellationToken);
        var observed = state.ObservePost(catalog.Id, dateTimeProvider.UtcNow);
        if (observed.IsFailure)
            return observed;

        return Result.Success();
    }
}
