using FluentAssertions;
using Venture.Application.Abstractions.Events;
using Venture.Application.Abstractions.Integration;
using Venture.Application.Integration;
using Venture.Application.Social.EventHandlers;
using Venture.Domain.Social.Follows.Events;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Tests.Social;

public class PublishUserFollowedHandlerTests
{
    [Fact]
    public async Task Handle_EnqueuesFollowFact_UntilCommitActionsRun()
    {
        var followerId = Guid.Parse("1b2c3d4e-5f60-4718-92ab-c3d4e5f60718");
        var followingId = Guid.Parse("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c");
        var actions = new RecordingPostCommitActions();
        var facts = new RecordingFollowFacts();
        var handler = new PublishUserFollowedHandler(actions, facts);

        await handler.Handle(Notification(followerId, followingId), CancellationToken.None);

        facts.Published.Should().BeEmpty();

        await actions.RunAsync(CancellationToken.None);

        facts.Published.Should().Equal((followerId, followingId));
    }

    private static DomainEventNotification<UserFollowedDomainEvent> Notification(Guid followerId, Guid followingId) =>
        new(new UserFollowedDomainEvent(UserId.From(followerId).Value, UserId.From(followingId).Value));

    private sealed class RecordingFollowFacts : IFollowFacts
    {
        public List<(Guid FollowerId, Guid FollowingId)> Published { get; } = [];

        public Task PublishUserFollowedAsync(
            Guid followerId,
            Guid followingId,
            CancellationToken cancellationToken)
        {
            Published.Add((followerId, followingId));
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingPostCommitActions : IPostCommitActions
    {
        private readonly List<Func<CancellationToken, Task>> _actions = [];

        public void Enqueue(Func<CancellationToken, Task> action) => _actions.Add(action);

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            foreach (var action in _actions)
                await action(cancellationToken);
        }
    }
}
