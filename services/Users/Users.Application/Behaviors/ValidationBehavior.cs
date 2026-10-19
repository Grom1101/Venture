using FluentValidation;
using MediatR;
using Venture.Application.Common.Results;
using Venture.Domain.Common.Results;

namespace Venture.Application.Behaviors;

/// <summary>
/// FluentValidation проверяет формат входных данных. Бизнес-инварианты остаются в домене.
/// </summary>
public sealed class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var applicableValidators = validators.ToArray();

        if (applicableValidators.Length == 0)
            return await next(cancellationToken);

        var context = new ValidationContext<TRequest>(request);

        var failures = (await Task.WhenAll(
                applicableValidators.Select(validator => validator.ValidateAsync(context, cancellationToken))))
            .SelectMany(result => result.Errors)
            .Where(failure => failure is not null)
            .Select(failure => Error.Validation(failure.PropertyName, failure.ErrorMessage))
            .ToArray();

        if (failures.Length == 0)
            return await next(cancellationToken);

        return CreateValidationResult(new ValidationError(failures));
    }

    private static TResponse CreateValidationResult(ValidationError error)
    {
        if (typeof(TResponse) == typeof(Result))
            return (TResponse)Result.Failure(error);

        var valueType = typeof(TResponse).GetGenericArguments()[0];

        var failureMethod = typeof(Result)
            .GetMethods()
            .First(method => method is { Name: nameof(Result.Failure), IsGenericMethodDefinition: true })
            .MakeGenericMethod(valueType);

        return (TResponse)failureMethod.Invoke(null, [error])!;
    }
}
