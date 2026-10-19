using System.Text.Json;
using FluentAssertions;
using Venture.Infrastructure.Messaging;

namespace Venture.Application.Tests.Messaging;

public class PostPublishedMessageTests
{
    private static readonly Guid PostId = Guid.Parse("2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11");
    private static readonly Guid AuthorId = Guid.Parse("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c");
    private static readonly DateTime OccurredAtUtc = new(2026, 10, 12, 14, 0, 0, DateTimeKind.Utc);

    private const string CanonicalJson = """
        {
          "schemaVersion": 1,
          "eventId": "2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11",
          "occurredAtUtc": "2026-10-12T14:00:00Z",
          "postId": "2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11",
          "authorId": "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c"
        }
        """;

    [Fact]
    public void Plan_UsesPostIdAsEventId_AndAuthorIdAsKey()
    {
        var publication = PostPublishedFact.Plan(PostId, AuthorId, OccurredAtUtc);

        publication.Topic.Should().Be("venture.posts.post-published");
        publication.Key.Should().Be("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c");
        publication.Message.SchemaVersion.Should().Be(1);
        publication.Message.EventId.Should().Be(PostId);
        publication.Message.PostId.Should().Be(PostId);
        publication.Message.AuthorId.Should().Be(AuthorId);
        publication.Message.OccurredAtUtc.Should().Be(OccurredAtUtc);
    }

    [Fact]
    public void Serialize_MatchesAgreedPropertyNamesAndValues()
    {
        var publication = PostPublishedFact.Plan(PostId, AuthorId, OccurredAtUtc);
        var serialized = JsonSerializer.Serialize(publication.Message, KafkaJson.Options);

        using var actual = JsonDocument.Parse(serialized);
        using var expected = JsonDocument.Parse(CanonicalJson);

        PropertyNames(actual).Should().Equal("schemaVersion", "eventId", "occurredAtUtc", "postId", "authorId");
        PropertyNames(expected).Should().Equal(PropertyNames(actual));
        JsonEquals(actual.RootElement, expected.RootElement).Should().BeTrue();
    }

    [Fact]
    public void Deserialize_ReadsCanonicalJson_AndIgnoresUnknownFields()
    {
        var message = JsonSerializer.Deserialize<PostPublishedMessage>(CanonicalJson, KafkaJson.Options);

        message.Should().NotBeNull();
        message!.SchemaVersion.Should().Be(1);
        message.EventId.Should().Be(PostId);
        message.PostId.Should().Be(PostId);
        message.AuthorId.Should().Be(AuthorId);
        message.OccurredAtUtc.Should().Be(OccurredAtUtc);

        const string withExtraField = """
            {
              "schemaVersion": 1,
              "eventId": "2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11",
              "occurredAtUtc": "2026-10-12T14:00:00Z",
              "postId": "2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11",
              "authorId": "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c",
              "text": "не входит в факт"
            }
            """;

        var tolerant = JsonSerializer.Deserialize<PostPublishedMessage>(withExtraField, KafkaJson.Options);
        tolerant.Should().Be(message);
    }

    private static string[] PropertyNames(JsonDocument document) =>
        document.RootElement.EnumerateObject().Select(property => property.Name).ToArray();

    private static bool JsonEquals(JsonElement left, JsonElement right)
    {
        if (left.ValueKind != right.ValueKind)
            return false;

        if (left.ValueKind != JsonValueKind.Object)
            return left.GetRawText() == right.GetRawText();

        var leftProperties = left.EnumerateObject().ToArray();
        var rightProperties = right.EnumerateObject().ToArray();

        if (leftProperties.Length != rightProperties.Length)
            return false;

        for (var index = 0; index < leftProperties.Length; index++)
        {
            if (leftProperties[index].Name != rightProperties[index].Name)
                return false;

            if (!JsonEquals(leftProperties[index].Value, rightProperties[index].Value))
                return false;
        }

        return true;
    }
}
