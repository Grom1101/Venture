using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Venture.Application.Abstractions.Events;
using Venture.Application.Abstractions.Time;
using Venture.Application.Achievements.Facts;
using Venture.Domain.Abstractions.Events;
using Venture.Domain.Achievements;
using Venture.Domain.Achievements.Enums;
using Venture.Domain.Users.ValueObjects;
using Venture.Infrastructure.Persistence;
using Venture.Infrastructure.Persistence.Repositories;

namespace Venture.IntegrationTests.Facts;

public class AchievementFactTests
{
    [Fact]
    public async Task FirstPostFact_GrantsTheAchievement()
    {
        await using var database = await ProgressDatabase.CreateAsync();
        await database.SeedCatalogAsync();
        var authorId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        await database.CommitPostAsync(authorId, postId);

        var progress = await database.LoadAsync(authorId);
        progress.PostsObserved.Should().Be(1);
        progress.Earned.Should().ContainSingle(earned => earned.AchievementId == database.FirstPostId);
    }

    [Fact]
    public async Task AnotherPost_DoesNotGrantTheAchievementAgain()
    {
        await using var database = await ProgressDatabase.CreateAsync();
        await database.SeedCatalogAsync();
        var authorId = Guid.NewGuid();

        await database.CommitPostAsync(authorId, Guid.NewGuid());
        await database.CommitPostAsync(authorId, Guid.NewGuid());

        var progress = await database.LoadAsync(authorId);
        progress.PostsObserved.Should().Be(2);
        progress.Earned.Should().ContainSingle();
    }

    [Fact]
    public async Task SameEventId_DoesNotIncreaseTheCounter()
    {
        await using var database = await ProgressDatabase.CreateAsync();
        await database.SeedCatalogAsync();
        var authorId = Guid.NewGuid();
        var postId = Guid.NewGuid();

        await database.CommitPostAsync(authorId, postId);
        await database.CommitPostAsync(authorId, postId);

        var progress = await database.LoadAsync(authorId);
        progress.PostsObserved.Should().Be(1);
        var storedFacts = await database.Db.ProcessedFacts.CountAsync();
        storedFacts.Should().Be(1);
    }

    [Fact]
    public async Task FollowFact_DoesNotGrantThePostAchievement()
    {
        await using var database = await ProgressDatabase.CreateAsync();
        await database.SeedCatalogAsync();
        var followingId = Guid.NewGuid();
        var followerId = Guid.NewGuid();

        await database.CommitFollowAsync(followerId, followingId);

        var progress = await database.LoadAsync(followingId);
        progress.PostsObserved.Should().Be(0);
        progress.FollowersObserved.Should().Be(1);
        progress.Earned.Should().ContainSingle(earned => earned.AchievementId == database.FirstFollowerId);
        progress.Earned.Should().NotContain(earned => earned.AchievementId == database.FirstPostId);
    }

    [Fact]
    public async Task RolledBackFact_CanBeAcceptedOnTheNextAttempt()
    {
        await using var database = await ProgressDatabase.CreateAsync();
        await database.SeedCatalogAsync();
        var authorId = Guid.NewGuid();
        var postId = Guid.NewGuid();
        var handler = database.PostHandler();

        await using (var transaction = await database.Db.Database.BeginTransactionAsync())
        {
            await handler.Handle(
                new ObservePostPublishedCommand(postId.ToString("D"), postId, authorId, 1),
                CancellationToken.None);
            await database.Db.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        database.Db.ChangeTracker.Clear();
        await database.CommitPostAsync(authorId, postId);

        var progress = await database.LoadAsync(authorId);
        progress.PostsObserved.Should().Be(1);
        (await database.Db.ProcessedFacts.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task EarnedPair_IsUniqueInTheDatabase()
    {
        await using var database = await ProgressDatabase.CreateAsync();
        var userId = Guid.NewGuid();
        var achievementId = Guid.NewGuid();
        var earnedOn = DateTime.UtcNow;

        await database.Db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "UserAchievementStates" ("UserId", "PostsObserved", "FollowersObserved")
             VALUES ({userId}, 1, 0)
             """);
        await database.Db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "EarnedAchievements" ("UserId", "AchievementId", "EarnedOnUtc")
             VALUES ({userId}, {achievementId}, {earnedOn})
             """);

        var duplicate = () => database.Db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "EarnedAchievements" ("UserId", "AchievementId", "EarnedOnUtc")
             VALUES ({userId}, {achievementId}, {earnedOn})
             """);

        await duplicate.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public void PostgresProvider_LocksTheProgressRow()
    {
        AchievementProgressStore.LocksRows("Npgsql.EntityFrameworkCore.PostgreSQL").Should().BeTrue();
        AchievementProgressStore.LocksRows("Microsoft.EntityFrameworkCore.Sqlite").Should().BeFalse();
    }
}

internal sealed class ProgressDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private ProgressDatabase(SqliteConnection connection, VentureDbContext dbContext)
    {
        _connection = connection;
        Db = dbContext;
        Achievements = new AchievementRepository(dbContext);
        Store = new AchievementProgressStore(dbContext);
        Clock = new FixedClock(new DateTime(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc));
    }

    public VentureDbContext Db { get; }

    public AchievementRepository Achievements { get; }

    public AchievementProgressStore Store { get; }

    public FixedClock Clock { get; }

    public Venture.Domain.Achievements.ValueObjects.AchievementId FirstPostId { get; private set; } = null!;

    public Venture.Domain.Achievements.ValueObjects.AchievementId FirstFollowerId { get; private set; } = null!;

    public static async Task<ProgressDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<VentureDbContext>()
            .UseSqlite(connection)
            .Options;

        var dbContext = new VentureDbContext(options, new NoEvents());
        await dbContext.Database.EnsureCreatedAsync();

        return new ProgressDatabase(connection, dbContext);
    }

    public async Task SeedCatalogAsync()
    {
        var firstPost = Achievement.Create(
            AchievementType.FirstSteps,
            "Первый пост",
            "Опубликован первый пост",
            Clock.UtcNow).Value;
        var firstFollower = Achievement.Create(
            AchievementType.FirstSteps,
            "Первый подписчик",
            "На вас подписался первый пользователь",
            Clock.UtcNow).Value;

        Db.Achievements.AddRange(firstPost, firstFollower);
        await Db.SaveChangesAsync();

        FirstPostId = firstPost.Id;
        FirstFollowerId = firstFollower.Id;
    }

    public ObservePostPublishedCommandHandler PostHandler() =>
        new(Achievements, Store, Clock);

    public async Task CommitPostAsync(Guid authorId, Guid postId)
    {
        await using var transaction = await Db.Database.BeginTransactionAsync();
        await PostHandler().Handle(
            new ObservePostPublishedCommand(postId.ToString("D"), postId, authorId, 1),
            CancellationToken.None);
        await Db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public async Task CommitFollowAsync(Guid followerId, Guid followingId)
    {
        var eventId = $"{followingId:D}:{followerId:D}";
        var handler = new ObserveUserFollowedCommandHandler(Achievements, Store, Clock);

        await using var transaction = await Db.Database.BeginTransactionAsync();
        await handler.Handle(
            new ObserveUserFollowedCommand(eventId, followerId, followingId, 1),
            CancellationToken.None);
        await Db.SaveChangesAsync();
        await transaction.CommitAsync();
    }

    public Task<UserAchievementProgress> LoadAsync(Guid userId) =>
        Db.UserAchievementStates
            .Include(progress => progress.Earned)
            .SingleAsync(progress => progress.Id == UserId.From(userId).Value);

    public ValueTask DisposeAsync()
    {
        Db.Dispose();
        return _connection.DisposeAsync();
    }

    private sealed class NoEvents : IDomainEventDispatcher
    {
        public Task DispatchAsync(IReadOnlyCollection<IDomainEvent> domainEvents, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }
}

internal sealed class FixedClock(DateTime utcNow) : IDateTimeProvider
{
    public DateTime UtcNow => utcNow;
}
