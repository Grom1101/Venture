namespace Venture.Application.Achievements.Facts;

public sealed class UnsupportedFactSchemaException(int schemaVersion, string? eventId)
    : Exception($"Неподдерживаемая версия {schemaVersion} факта {eventId}.")
{
    public int SchemaVersion { get; } = schemaVersion;

    public string? EventId { get; } = eventId;
}
