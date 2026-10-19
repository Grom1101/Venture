using Venture.Application.Common.Results;
using Venture.Domain.Common.Results;

namespace Venture.Api.Extensions;

public static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        if (result.IsSuccess)
            throw new InvalidOperationException("Успешный результат нельзя превратить в ошибку");

        return result.Error.Type switch
        {
            ErrorType.Validation => Results.Problem(
                title: "Ошибка валидации",
                detail: result.Error.Description,
                statusCode: StatusCodes.Status400BadRequest,
                extensions: Extensions(result.Error)),
            ErrorType.Unauthorized => Results.Problem(
                title: "Не авторизован",
                detail: result.Error.Description,
                statusCode: StatusCodes.Status401Unauthorized,
                extensions: Extensions(result.Error)),
            ErrorType.Forbidden => Results.Problem(
                title: "Доступ запрещён",
                detail: result.Error.Description,
                statusCode: StatusCodes.Status403Forbidden,
                extensions: Extensions(result.Error)),
            ErrorType.NotFound => Results.Problem(
                title: "Не найдено",
                detail: result.Error.Description,
                statusCode: StatusCodes.Status404NotFound,
                extensions: Extensions(result.Error)),
            ErrorType.Conflict => Results.Problem(
                title: "Конфликт",
                detail: result.Error.Description,
                statusCode: StatusCodes.Status409Conflict,
                extensions: Extensions(result.Error)),
            _ => Results.Problem(
                title: "Ошибка сервера",
                detail: result.Error.Description,
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: Extensions(result.Error))
        };
    }

    public static IResult Match<TValue>(this Result<TValue> result, Func<TValue, IResult> onSuccess) =>
        result.IsSuccess ? onSuccess(result.Value) : result.ToProblem();

    public static IResult Match(this Result result, Func<IResult> onSuccess) =>
        result.IsSuccess ? onSuccess() : result.ToProblem();

    private static Dictionary<string, object?> Extensions(Error error)
    {
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };

        if (error is ValidationError validationError)
            extensions["errors"] = validationError.Errors
                .Select(item => new { item.Code, item.Description })
                .ToArray();

        return extensions;
    }
}
