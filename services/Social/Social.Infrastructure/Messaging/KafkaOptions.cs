namespace Venture.Infrastructure.Messaging;

public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; init; } = string.Empty;

    public string ClientId { get; init; } = string.Empty;

    public string GroupId { get; init; } = string.Empty;
}
