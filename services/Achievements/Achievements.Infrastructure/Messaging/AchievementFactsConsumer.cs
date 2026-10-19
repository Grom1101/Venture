using Confluent.Kafka;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Venture.Application.Achievements.Facts;

namespace Venture.Infrastructure.Messaging;

/// <summary>
/// Смещение коммитится после коммита базы. Нечитаемый факт пропускается.
/// Неподдерживаемая версия и ошибка базы смещение не двигают.
/// </summary>
internal sealed class AchievementFactsConsumer(
    IOptions<KafkaOptions> options,
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    ILogger<AchievementFactsConsumer> logger) : BackgroundService
{
    private static readonly string[] Topics =
    [
        KafkaTopics.PostPublished,
        KafkaTopics.UserFollowed
    ];

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
                consumer.Subscribe(Topics);
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

            FactOffset.ApplyAsync(
                cancellationToken => HandleAsync(result.Topic, result.Message.Value, cancellationToken),
                () => consumer.Commit(result),
                logger,
                result.Topic,
                stoppingToken).GetAwaiter().GetResult();
        }
    }

    private async Task HandleAsync(string topic, string payload, CancellationToken cancellationToken)
    {
        var fact = FactParser.Parse(topic, payload);

        logger.LogInformation(
            "Факт {EventId} пользователя {UserId} читается из топика {Topic}",
            fact.EventId,
            fact.UserId,
            topic);

        using var scope = scopeFactory.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(fact.Command, cancellationToken);
    }

    private IConsumer<string, string> CreateConsumer()
    {
        var kafka = options.Value;

        return new ConsumerBuilder<string, string>(new ConsumerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            GroupId = kafka.GroupId,
            ClientId = string.IsNullOrWhiteSpace(kafka.ClientId) ? "venture-achievements" : kafka.ClientId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
            EnableAutoOffsetStore = false,
            SocketTimeoutMs = 10_000
        }).Build();
    }
}
