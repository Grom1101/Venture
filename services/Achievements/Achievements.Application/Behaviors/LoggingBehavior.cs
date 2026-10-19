using MediatR;
using Microsoft.Extensions.Logging;
using Venture.Domain.Common.Results;

namespace Venture.Application.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Обработка {RequestName}", requestName);

        var response = await next(cancellationToken);

        if (response.IsSuccess)
            logger.LogInformation("{RequestName} обработан успешно", requestName);
        else
            logger.LogWarning("{RequestName} завершился ошибкой {ErrorCode}: {ErrorDescription}",
                requestName, response.Error.Code, response.Error.Description);

        return response;
    }
}
