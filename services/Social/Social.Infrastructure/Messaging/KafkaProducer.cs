using System.Text.Json;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Venture.Infrastructure.Messaging;

internal sealed class KafkaProducer : IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        _logger = logger;

        var kafka = options.Value;

        if (string.IsNullOrWhiteSpace(kafka.BootstrapServers))
            throw new InvalidOperationException("Kafka:BootstrapServers is not configured.");

        _producer = new ProducerBuilder<string, string>(new ProducerConfig
        {
            BootstrapServers = kafka.BootstrapServers,
            ClientId = string.IsNullOrWhiteSpace(kafka.ClientId) ? "venture" : kafka.ClientId,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 10_000
        }).Build();
    }

    public async Task PublishAsync<T>(
        string topic,
        string key,
        T message,
        CancellationToken cancellationToken)
    {
        var delivery = await _producer.ProduceAsync(
            topic,
            new Message<string, string>
            {
                Key = key,
                Value = JsonSerializer.Serialize(message, KafkaJson.Options)
            },
            cancellationToken);

        if (delivery.Status == PersistenceStatus.NotPersisted)
            throw new InvalidOperationException($"Kafka did not persist the message to topic '{topic}'.");

        _logger.LogInformation("Сообщение записано в топик {Topic}", topic);
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(10));
        _producer.Dispose();
    }
}
