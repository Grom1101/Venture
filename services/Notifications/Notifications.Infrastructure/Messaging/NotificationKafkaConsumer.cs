using System.Text.Json;
using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Venture.Application.Notifications.Commands.CreateNotification;

namespace Venture.Infrastructure.Messaging;

/// <summary>
/// Смещение коммитится после обработки. Сообщение, которое нельзя прочитать, пропускается.
/// Ошибка базы смещение не двигает: то же сообщение придёт снова.
/// </summary>
internal sealed class NotificationKafkaConsumer(
    IOptions<KafkaOptions> options,
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<NotificationKafkaConsumer> logger) : BackgroundService
{
    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (environment.IsEnvironment("Testing"))
            return Task.CompletedTask;

        if (string.IsNullOrWhiteSpace(options.Value.BootstrapServers))
            throw new InvalidOperationException("Kafka:BootstrapServers is not configured.");

        if (string.IsNullOrWhiteSpace(options.Value.GroupId))
            throw new InvalidOperationException("Kafka:GroupId is not configured.");

        return Task.Run(() => Run(stoppingToken));
    }

    private void Run(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var consumer = CreateConsumer();
                consumer.Subscribe(KafkaTopics.Notifications);
                Read(consumer, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Чтение Kafka прервалось, повтор через 5 секунд");

                try
                {
                    Task.Delay(TimeSpan.FromSeconds(5), stoppingToken).GetAwaiter().GetResult();
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    private void Read(IConsumer<string, string> consumer, CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var result = consumer.Consume(TimeSpan.FromSeconds(1));

            if (result is null)
                continue;

            if (string.IsNullOrWhiteSpace(result.Message?.Value))
            {
                logger.LogError("Пустое сообщение в топике {Topic}", result.Topic);
                consumer.Commit(result);
                continue;
            }

            try
            {
                HandleAsync(result.Message.Value, stoppingToken).GetAwaiter().GetResult();
            }
            catch (JsonException exception)
            {
                logger.LogError(exception, "Сообщение из топика {Topic} пропущено: его нельзя прочитать", result.Topic);
            }

            consumer.Commit(result);
        }
    }

    private async Task HandleAsync(string payload, CancellationToken cancellationToken)
    {
        var message = JsonSerializer.Deserialize<NotificationMessage>(payload, KafkaJson.Options)
            ?? throw new JsonException("Пустое тело сообщения.");

        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        var result = await sender.Send(
            new CreateNotificationCommand(
                message.RecipientId,
                message.Type,
                message.ActorId,
                message.ReferenceId,
                message.CreatedAtUtc),
            cancellationToken);

        if (result.IsFailure)
            logger.LogWarning(
                "Уведомление не записано: {ErrorCode}: {ErrorDescription}",
                result.Error.Code,
                result.Error.Description);
    }

    private IConsumer<string, string> CreateConsumer()
    {
        var kafka = options.Value;

        return new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            GroupId = kafka.GroupId,
            ClientId = string.IsNullOrWhiteSpace(kafka.ClientId) ? "venture" : kafka.ClientId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            SocketTimeoutMs = 10_000
        }).Build();
    }
}
