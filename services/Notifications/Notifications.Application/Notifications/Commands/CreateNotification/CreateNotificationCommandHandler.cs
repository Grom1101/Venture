using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Notifications;
using Venture.Domain.Common.Results;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Notifications.Commands.CreateNotification;

public sealed record CreateNotificationCommand(
    Guid RecipientId,
    string Type,
    Guid? ActorId,
    Guid? ReferenceId,
    DateTime CreatedAtUtc) : ICommand;

internal sealed class CreateNotificationCommandHandler(INotificationWriter notificationWriter)
    : ICommandHandler<CreateNotificationCommand>
{
    public async Task<Result> Handle(CreateNotificationCommand command, CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<NotificationType>(command.Type, out var type))
            return Result.Failure(Error.Validation("Type", "Неизвестный тип уведомления"));

        var recipientId = UserId.From(command.RecipientId);
        if (recipientId.IsFailure)
            return Result.Failure(recipientId.Error);

        UserId? actor = null;
        if (command.ActorId is { } actorId)
        {
            var actorResult = UserId.From(actorId);
            if (actorResult.IsFailure)
                return Result.Failure(actorResult.Error);

            actor = actorResult.Value;
        }

        await notificationWriter.AddAsync(
            recipientId.Value,
            type,
            actor,
            command.ReferenceId,
            command.CreatedAtUtc,
            cancellationToken);

        return Result.Success();
    }
}
