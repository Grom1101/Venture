namespace Venture.Application.Abstractions.Integration;

/// <summary>
/// Действия в чужих сервисах выполняются после коммита своей транзакции.
/// </summary>
public interface IPostCommitActions
{
    void Enqueue(Func<CancellationToken, Task> action);

    Task RunAsync(CancellationToken cancellationToken);
}
