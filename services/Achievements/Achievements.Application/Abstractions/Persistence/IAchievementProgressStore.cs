using Venture.Domain.Achievements;
using Venture.Domain.Users.ValueObjects;

namespace Venture.Application.Abstractions.Persistence;

public interface IAchievementProgressStore
{
    /// <summary>
    /// true, если EventId записан впервые. false — этот факт уже учтён, состояние менять нельзя.
    /// </summary>
    Task<bool> TryAcceptAsync(string eventId, DateTime acceptedOnUtc, CancellationToken cancellationToken);

    Task<UserAchievementProgress> LockAsync(UserId userId, CancellationToken cancellationToken);
}
