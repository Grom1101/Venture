using Venture.Application.Abstractions.Messaging;

namespace Venture.Application.Achievements.Facts;

public sealed record ObservePostPublishedCommand(
    string EventId,
    Guid PostId,
    Guid AuthorId,
    int SchemaVersion) : ICommand;
