namespace Venture.Infrastructure.Messaging;

internal sealed record NotificationMessage(
    Guid RecipientId,
    string Type,
    Guid? ActorId,
    Guid? ReferenceId,
    DateTime CreatedAtUtc);
