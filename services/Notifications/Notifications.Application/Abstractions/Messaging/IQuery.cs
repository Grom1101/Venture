using MediatR;
using Venture.Domain.Common.Results;

namespace Venture.Application.Abstractions.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
