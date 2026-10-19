namespace Venture.Infrastructure.Persistence;

/// <summary>
/// Защита от повторной доставки. Это не достижение и не прогресс.
/// </summary>
public sealed class ProcessedFact
{
    public string EventId { get; private set; } = null!;

    public DateTime AcceptedOnUtc { get; private set; }

    private ProcessedFact()
    {
    }

    public ProcessedFact(string eventId, DateTime acceptedOnUtc)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventId);

        EventId = eventId;
        AcceptedOnUtc = acceptedOnUtc;
    }
}
