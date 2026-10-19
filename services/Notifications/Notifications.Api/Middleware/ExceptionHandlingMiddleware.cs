using Microsoft.AspNetCore.Mvc;

namespace Venture.Api.Middleware;

/// <summary>
/// Ожидаемые ошибки возвращаются через Result. Сюда долетают только
/// непредвиденные сбои.
/// </summary>
public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Необработанная ошибка");

            await WriteProblemAsync(
                context,
                StatusCodes.Status500InternalServerError,
                "Ошибка сервера",
                "Произошла непредвиденная ошибка");
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
            return;

        context.Response.StatusCode = statusCode;

        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = $"{context.Request.Method} {context.Request.Path}"
        });
    }
}
