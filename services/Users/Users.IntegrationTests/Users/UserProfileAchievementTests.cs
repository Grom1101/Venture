using FluentAssertions;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Venture.Application.Abstractions.Authentication;
using Venture.Application.Abstractions.Caching;
using Venture.Application.Abstractions.Events;
using Venture.Application.Achievements;
using Venture.Application.Social;
using Venture.Application.Users.Queries.GetMyProfile;
using Venture.Application.Users.Queries.GetUserProfile;
using Venture.Contracts.Achievements;
using Venture.Domain.Abstractions.Events;
using Venture.Domain.Users;
using Venture.Domain.Users.ValueObjects;
using Venture.Infrastructure.Integration;
using Venture.Infrastructure.Persistence;

namespace Venture.IntegrationTests.Users;

public class UserProfileAchievementTests
{
    [Fact]
    public async Task GetMyProfile_FillsAchievements_AndDoesNotCacheThem()
    {
        await using var database = await ProfileDatabase.CreateAsync();
        var user = await database.AddUserAsync("annadoe");
        var cache = new RecordingCache();
        var display = new ScriptedDisplay();
        var first = Achievement("Первый пост", new DateTime(2026, 5, 1, 0, 0, 0, DateTimeKind.Utc));
        display.Next = [first];
        var handler = new GetMyProfileQueryHandler(
            database.Db,
            new FixedUser(user.Id),
            new FixedStats(2, 3),
            display,
            cache);

        var firstRead = await handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        firstRead.IsSuccess.Should().BeTrue();
        firstRead.Value.Achievements.Should().ContainSingle();
        firstRead.Value.Achievements.Single().Name.Should().Be("Первый пост");
        firstRead.Value.FollowersCount.Should().Be(2);
        CachedAchievements(cache).Should().BeEmpty();

        var second = Achievement("Первый подписчик", new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc));
        display.Next = [second, first];

        var secondRead = await handler.Handle(new GetMyProfileQuery(), CancellationToken.None);

        secondRead.Value.Achievements.Select(item => item.Name).Should().Equal("Первый подписчик", "Первый пост");
        display.Calls.Should().Be(2);
        CachedAchievements(cache).Should().BeEmpty();
    }

    [Fact]
    public async Task GetUserProfile_UsesTheDisplayContract_OnEveryRead()
    {
        await using var database = await ProfileDatabase.CreateAsync();
        await database.AddUserAsync("borisdoe");
        var cache = new RecordingCache();
        var display = new ScriptedDisplay { Next = [] };
        var handler = new GetUserProfileQueryHandler(database.Db, new FixedStats(0, 1), display, cache);

        var empty = await handler.Handle(new GetUserProfileQuery("borisdoe"), CancellationToken.None);

        empty.IsSuccess.Should().BeTrue();
        empty.Value.Achievements.Should().BeEmpty();
        empty.Value.FollowingCount.Should().Be(1);

        display.Next = [Achievement("Первый пост", new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc))];
        var filled = await handler.Handle(new GetUserProfileQuery("borisdoe"), CancellationToken.None);

        filled.Value.Achievements.Should().ContainSingle();
        display.Calls.Should().Be(2);
        CachedAchievements(cache).Should().BeEmpty();
    }

    [Fact]
    public void DisplayClientTimeout_IsTwoSeconds()
    {
        AchievementDisplayClient.Timeout.Should().Be(TimeSpan.FromSeconds(2));
    }

    [Fact]
    public async Task DisplayClient_ReturnsAchievements_WhenTheContractResponds()
    {
        var earnedOnUtc = new DateTime(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc);
        var client = new ScriptedAchievements
        {
            Reply = new AchievementsReply
            {
                Achievements =
                {
                    new UserAchievement
                    {
                        Id = Guid.NewGuid().ToString("D"),
                        Type = "FirstPost",
                        Name = "Первый пост",
                        Description = "Опубликован первый пост",
                        EarnedOnUtc = Timestamp.FromDateTime(earnedOnUtc)
                    }
                }
            }
        };

        var achievements = await Client(client).GetAsync(Guid.NewGuid(), CancellationToken.None);

        achievements.Should().ContainSingle();
        achievements.Single().Type.Should().Be("FirstPost");
        achievements.Single().Name.Should().Be("Первый пост");
        achievements.Single().EarnedOnUtc.Should().Be(earnedOnUtc);
        client.LastOptions.Deadline.Should().NotBeNull();
        (client.LastOptions.Deadline!.Value - DateTime.UtcNow)
            .Should().BeCloseTo(AchievementDisplayClient.Timeout, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task DisplayClient_ReturnsEmpty_WhenThereAreNoAchievements()
    {
        var achievements = await Client(new ScriptedAchievements()).GetAsync(Guid.NewGuid(), CancellationToken.None);

        achievements.Should().BeEmpty();
    }

    [Fact]
    public async Task DisplayClient_ReturnsEmpty_WhenAchievementsFails()
    {
        var client = new ScriptedAchievements
        {
            Error = new RpcException(new Status(StatusCode.Unavailable, "Achievements недоступен"))
        };

        var achievements = await Client(client).GetAsync(Guid.NewGuid(), CancellationToken.None);

        achievements.Should().BeEmpty();
    }

    [Fact]
    public async Task DisplayClient_ReturnsEmpty_WhenTheCallTimesOut()
    {
        var client = new ScriptedAchievements
        {
            Error = new RpcException(new Status(StatusCode.DeadlineExceeded, "дедлайн"))
        };

        var achievements = await Client(client).GetAsync(Guid.NewGuid(), CancellationToken.None);

        achievements.Should().BeEmpty();
    }

    [Fact]
    public async Task DisplayClient_PropagatesCallerCancellation()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var client = new ScriptedAchievements
        {
            Error = new RpcException(new Status(StatusCode.Cancelled, "отменено"))
        };

        var act = () => Client(client).GetAsync(Guid.NewGuid(), cancellation.Token);

        await act.Should().ThrowAsync<RpcException>();
    }

    private static UserAchievementDto Achievement(string name, DateTime earnedOnUtc) =>
        new(Guid.NewGuid(), "FirstPost", name, "Опубликован первый пост", earnedOnUtc);

    private static IReadOnlyCollection<UserAchievementDto> CachedAchievements(RecordingCache cache) =>
        cache.Items.Values.Cast<UserProfileDto>().SelectMany(profile => profile.Achievements).ToArray();

    private static GrpcUserAchievementDisplay Client(ScriptedAchievements client) =>
        new(client, NullLogger<GrpcUserAchievementDisplay>.Instance);

    private sealed class ScriptedAchievements : AchievementLookup.AchievementLookupClient
    {
        public AchievementsReply Reply { get; set; } = new();

        public Exception? Error { get; set; }

        public CallOptions LastOptions { get; private set; }

        public override AsyncUnaryCall<AchievementsReply> GetUserAchievementsAsync(
            UserIdRequest request,
            CallOptions options)
        {
            LastOptions = options;
            var response = Error is null
                ? Task.FromResult(Reply)
                : Task.FromException<AchievementsReply>(Error);

            return new AsyncUnaryCall<AchievementsReply>(
                response,
                Task.FromResult(new Metadata()),
                () => Status.DefaultSuccess,
                () => new Metadata(),
                () => { });
        }
    }

    private sealed class ScriptedDisplay : IUserAchievementDisplay
    {
        public IReadOnlyCollection<UserAchievementDto> Next { get; set; } = [];

        public int Calls { get; private set; }

        public Task<IReadOnlyCollection<UserAchievementDto>> GetAsync(Guid userId, CancellationToken cancellationToken)
        {
            Calls++;
            return Task.FromResult(Next);
        }
    }

    private sealed class FixedStats(int followers, int following) : IFollowStats
    {
        public Task<FollowStats> GetAsync(UserId userId, CancellationToken cancellationToken) =>
            Task.FromResult(new FollowStats(followers, following));
    }

    private sealed class FixedUser(UserId userId) : ICurrentUser
    {
        public UserId? UserId => userId;

        public bool IsAuthenticated => true;
    }

    private sealed class RecordingCache : ICacheService
    {
        public Dictionary<string, object> Items { get; } = [];

        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
            where T : class =>
            Task.FromResult(Items.TryGetValue(key, out var value) ? value as T : null);

        public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default)
            where T : class
        {
            Items[key] = value!;
            return Task.CompletedTask;
        }

        public Task RemoveAsync(string key, CancellationToken cancellationToken = default)
        {
            Items.Remove(key);
            return Task.CompletedTask;
        }

        public Task<long> GetCounterAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(0L);

        public Task<long> IncrementAsync(string key, CancellationToken cancellationToken = default) =>
            Task.FromResult(1L);
    }
}

internal sealed class ProfileDatabase : IAsyncDisposable
{
    private readonly SqliteConnection _connection;

    private ProfileDatabase(SqliteConnection connection, VentureDbContext dbContext)
    {
        _connection = connection;
        Db = dbContext;
    }

    public VentureDbContext Db { get; }

    public static async Task<ProfileDatabase> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<VentureDbContext>()
            .UseSqlite(connection)
            .Options;
        var dbContext = new VentureDbContext(options, new NoEvents());
        await dbContext.Database.EnsureCreatedAsync();
        return new ProfileDatabase(connection, dbContext);
    }

    public async Task<User> AddUserAsync(string username)
    {
        var user = User.Register(
            Username.Create(username).Value,
            Email.Create($"{username}@example.com").Value,
            PasswordHash.FromHash("salt.hash").Value,
            DisplayName.Create(username).Value,
            new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)).Value;

        Db.Users.Add(user);
        await Db.SaveChangesAsync();
        return user;
    }

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
