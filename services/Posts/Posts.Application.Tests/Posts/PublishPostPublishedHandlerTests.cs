using FluentAssertions;
using Venture.Application.Abstractions.Events;
using Venture.Application.Abstractions.Integration;
using Venture.Application.Integration;
using Venture.Application.Posts.EventHandlers;
using Venture.Domain.Posts.Events;
using Venture.Domain.Posts.ValueObjects;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Tests.Posts;

public class PublishPostPublishedHandlerTests
{
    [Fact]
    public async Task Handle_EnqueuesPostFact_UntilCommitActionsRun()
    {
        var postId = Guid.Parse("2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11");
        var authorId = Guid.Parse("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c");
        var actions = new RecordingPostCommitActions();
        var facts = new RecordingPostFacts();
        var handler = new PublishPostPublishedHandler(actions, facts);

        await handler.Handle(Notification(postId, authorId), CancellationToken.None);

        facts.Published.Should().BeEmpty();

        await actions.RunAsync(CancellationToken.None);

        facts.Published.Should().Equal((postId, authorId));
    }

    private static DomainEventNotification<PostPublishedDomainEvent> Notification(Guid postId, Guid authorId) =>
        new(new PostPublishedDomainEvent(PostId.From(postId).Value, UserId.From(authorId).Value));

    private sealed class RecordingPostFacts : IPostFacts
    {
        public List<(Guid PostId, Guid AuthorId)> Published { get; } = [];

        public Task PublishPostPublishedAsync(Guid postId, Guid authorId, CancellationToken cancellationToken)
        {
            Published.Add((postId, authorId));
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
