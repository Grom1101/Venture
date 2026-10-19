namespace Venture.Application.Abstractions.Transactions;

public interface ITransactionManager
{
    Task<ITransaction> BeginAsync(CancellationToken cancellationToken);
}

public interface ITransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}
