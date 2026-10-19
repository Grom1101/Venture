using System.Text.Json;

namespace Venture.Infrastructure.Messaging;

internal static class KafkaJson
{
    public static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);
}
