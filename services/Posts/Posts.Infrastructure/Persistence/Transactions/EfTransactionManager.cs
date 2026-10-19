using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Venture.Application.Abstractions.Transactions;

namespace Venture.Infrastructure.Persistence.Transactions;

internal sealed class EfTransactionManager(VentureDbContext dbContext) : ITransactionManager
{
    public async Task<ITransaction> BeginAsync(CancellationToken cancellationToken)
    {
        if (dbContext.Database.CurrentTransaction is not null)
            return new NestedTransaction();

        var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        return new EfTransaction(transaction);
    }

    private sealed class EfTransaction(IDbContextTransaction transaction) : ITransaction
    {
        private bool _completed;

        public async Task CommitAsync(CancellationToken cancellationToken)
        {
            await transaction.CommitAsync(cancellationToken);
            _completed = true;
        }

        public async Task RollbackAsync(CancellationToken cancellationToken)
        {
            if (_completed)
                return;

            await transaction.RollbackAsync(cancellationToken);
            _completed = true;
        }

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }

    /// <summary>
    /// Внешняя транзакция уже открыта (например, в интеграционном тесте) — вложенная ничего не делает.
    /// </summary>
    private sealed class NestedTransaction : ITransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
