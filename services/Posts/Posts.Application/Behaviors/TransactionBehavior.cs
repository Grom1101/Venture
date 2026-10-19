using MediatR;
using Venture.Application.Abstractions.Integration;
using Venture.Application.Abstractions.Messaging;
using Venture.Application.Abstractions.Persistence;
using Venture.Application.Abstractions.Transactions;
using Venture.Domain.Common.Results;

namespace Venture.Application.Behaviors;

/// <summary>
/// Транзакционная граница существует только для команд: запрос не меняет состояние,
/// поэтому ему не нужны ни транзакция, ни SaveChanges.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(
    ITransactionManager transactionManager,
    IUnitOfWork unitOfWork,
    IPostCommitActions postCommitActions)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IBaseCommand
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        await using var transaction = await transactionManager.BeginAsync(cancellationToken);

        TResponse response;

        try
        {
            response = await next(cancellationToken);

            if (response.IsFailure)
            {
                await transaction.RollbackAsync(cancellationToken);

                return response;
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);

            throw;
        }

        await postCommitActions.RunAsync(cancellationToken);

        return response;
    }
}
