using Venture.Application.Abstractions.Integration;

namespace Venture.Infrastructure.Integration;

internal sealed class PostCommitActions : IPostCommitActions
{
    private readonly List<Func<CancellationToken, Task>> _actions = [];

    public void Enqueue(Func<CancellationToken, Task> action) => _actions.Add(action);

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        foreach (var action in _actions)
            await action(cancellationToken);
    }
}
