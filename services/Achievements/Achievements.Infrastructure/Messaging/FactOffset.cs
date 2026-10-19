using System.Text.Json;
using Microsoft.Extensions.Logging;
using Venture.Application.Achievements.Facts;

namespace Venture.Infrastructure.Messaging;

internal static class FactOffset
{
    public static async Task ApplyAsync(
        Func<CancellationToken, Task> handle,
        Action commit,
        ILogger logger,
        string topic,
        CancellationToken cancellationToken)
    {
        try
        {
            await handle(cancellationToken);
            commit();
        }
        catch (Exception exception) when (exception is JsonException or InvalidFactException)
        {
            logger.LogError(exception, "Сообщение из топика {Topic} пропущено: его нельзя прочитать", topic);
            commit();
        }
        catch (UnsupportedFactSchemaException exception)
        {
            logger.LogError(
                exception,
                "Факт {EventId} из топика {Topic} отложен: версия {SchemaVersion} не поддерживается",
                exception.EventId,
                topic,
                exception.SchemaVersion);
            throw;
        }
    }
}
