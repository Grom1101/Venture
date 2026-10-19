using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Venture.Application.Achievements.Facts;
using Venture.Infrastructure.Messaging;

namespace Venture.IntegrationTests.Messaging;

public class FactContractTests
{
    private const string PostJson = """
        {
          "schemaVersion": 1,
          "eventId": "2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11",
          "occurredAtUtc": "2026-10-12T14:00:00Z",
          "postId": "2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11",
          "authorId": "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c"
        }
        """;

    private const string FollowJson = """
        {
          "schemaVersion": 1,
          "eventId": "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c:1b2c3d4e-5f60-4718-92ab-c3d4e5f60718",
          "occurredAtUtc": "2026-10-12T14:05:00Z",
          "followerId": "1b2c3d4e-5f60-4718-92ab-c3d4e5f60718",
          "followingId": "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c"
        }
        """;

    [Fact]
    public void PostPublishedJson_DeserializesIntoTheConsumerRecord()
    {
        var message = JsonSerializer.Deserialize<PostPublishedMessage>(PostJson, KafkaJson.Options);

        message.Should().NotBeNull();
        message!.SchemaVersion.Should().Be(1);
        message.EventId.Should().Be(message.PostId);
        message.AuthorId.Should().Be(Guid.Parse("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c"));

        var parsed = FactParser.Parse("venture.posts.post-published", PostJson);
        parsed.EventId.Should().Be("2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11");
        parsed.UserId.Should().Be(message.AuthorId);
        parsed.Command.Should().BeOfType<ObservePostPublishedCommand>();
    }

    [Fact]
    public void UserFollowedJson_DeserializesIntoTheConsumerRecord()
    {
        var message = JsonSerializer.Deserialize<UserFollowedMessage>(FollowJson, KafkaJson.Options);

        message.Should().NotBeNull();
        message!.EventId.Should().Be("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c:1b2c3d4e-5f60-4718-92ab-c3d4e5f60718");
        message.FollowingId.Should().Be(Guid.Parse("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c"));

        var parsed = FactParser.Parse("venture.social.user-followed", FollowJson);
        parsed.UserId.Should().Be(message.FollowingId);
        parsed.Command.Should().BeOfType<ObserveUserFollowedCommand>();
    }

    [Fact]
    public void UnknownField_IsIgnored()
    {
        const string json = """
            {
              "schemaVersion": 1,
              "eventId": "2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11",
              "occurredAtUtc": "2026-10-12T14:00:00Z",
              "postId": "2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11",
              "authorId": "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c",
              "text": "не входит в факт"
            }
            """;

        var parsed = FactParser.Parse("venture.posts.post-published", json);
        parsed.EventId.Should().Be("2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11");
    }

    [Fact]
    public void UnsupportedSchemaVersion_DoesNotLookLikeAnInvalidMessage()
    {
        const string json = """
            {
              "schemaVersion": 2,
              "eventId": "2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11",
              "occurredAtUtc": "2026-10-12T14:00:00Z",
              "postId": "2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11",
              "authorId": "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c"
            }
            """;

        var act = () => FactParser.Parse("venture.posts.post-published", json);
        act.Should().Throw<UnsupportedFactSchemaException>()
            .Which.SchemaVersion.Should().Be(2);
    }
}

public class FactOffsetTests
{
    [Fact]
    public async Task DatabaseError_DoesNotCommitTheOffset()
    {
        var committed = false;

        var act = () => FactOffset.ApplyAsync(
            _ => throw new DbUpdateException("database", new Exception("constraint")),
            () => committed = true,
            NullLogger.Instance,
            "venture.posts.post-published",
            CancellationToken.None);

        await act.Should().ThrowAsync<DbUpdateException>();
        committed.Should().BeFalse();
    }

    [Fact]
    public async Task InvalidJson_CommitsTheOffset()
    {
        var committed = false;

        await FactOffset.ApplyAsync(
            _ => throw new JsonException("broken"),
            () => committed = true,
            NullLogger.Instance,
            "venture.posts.post-published",
            CancellationToken.None);

        committed.Should().BeTrue();
    }

    [Fact]
    public async Task UnsupportedVersion_DoesNotCommitTheOffset()
    {
        var committed = false;

        var act = () => FactOffset.ApplyAsync(
            _ => throw new UnsupportedFactSchemaException(2, "2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11"),
            () => committed = true,
            NullLogger.Instance,
            "venture.posts.post-published",
            CancellationToken.None);

        await act.Should().ThrowAsync<UnsupportedFactSchemaException>();
        committed.Should().BeFalse();
    }

    [Fact]
    public async Task SuccessfulHandle_CommitsTheOffset()
    {
        var committed = false;

        await FactOffset.ApplyAsync(
            _ => Task.CompletedTask,
            () => committed = true,
            NullLogger.Instance,
            "venture.social.user-followed",
            CancellationToken.None);

        committed.Should().BeTrue();
    }
}
