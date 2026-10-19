using MediatR;
using Venture.Domain.Common.Results;

namespace Venture.Application.Abstractions.Messaging;

/// <summary>
/// Маркер, по которому TransactionBehavior отличает команды от запросов.
/// </summary>
public interface IBaseCommand;

public interface ICommand : IRequest<Result>, IBaseCommand;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;
