using System.Text.Json;
using FluentAssertions;
using Venture.Infrastructure.Messaging;

namespace Venture.Application.Tests.Messaging;

public class UserFollowedMessageTests
{
    private static readonly Guid FollowerId = Guid.Parse("1b2c3d4e-5f60-4718-92ab-c3d4e5f60718");
    private static readonly Guid FollowingId = Guid.Parse("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c");
    private static readonly DateTime OccurredAtUtc = new(2026, 10, 12, 14, 5, 0, DateTimeKind.Utc);

    private const string CanonicalJson = """
        {
          "schemaVersion": 1,
          "eventId": "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c:1b2c3d4e-5f60-4718-92ab-c3d4e5f60718",
          "occurredAtUtc": "2026-10-12T14:05:00Z",
          "followerId": "1b2c3d4e-5f60-4718-92ab-c3d4e5f60718",
          "followingId": "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c"
        }
        """;

    [Fact]
    public void Plan_UsesFollowingThenFollowerAsEventId_AndFollowingIdAsKey()
    {
        var publication = UserFollowedFact.Plan(FollowerId, FollowingId, OccurredAtUtc);

        publication.Topic.Should().Be("venture.social.user-followed");
        publication.Key.Should().Be("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c");
        publication.Message.SchemaVersion.Should().Be(1);
        publication.Message.EventId.Should().Be(
            "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c:1b2c3d4e-5f60-4718-92ab-c3d4e5f60718");
        publication.Message.FollowerId.Should().Be(FollowerId);
        publication.Message.FollowingId.Should().Be(FollowingId);
        publication.Message.OccurredAtUtc.Should().Be(OccurredAtUtc);
    }

    [Fact]
    public void Serialize_MatchesAgreedPropertyNamesAndValues()
    {
        var publication = UserFollowedFact.Plan(FollowerId, FollowingId, OccurredAtUtc);
        var serialized = JsonSerializer.Serialize(publication.Message, KafkaJson.Options);

        using var actual = JsonDocument.Parse(serialized);
        using var expected = JsonDocument.Parse(CanonicalJson);

        PropertyNames(actual).Should().Equal(
            "schemaVersion",
            "eventId",
            "occurredAtUtc",
            "followerId",
            "followingId");
        PropertyNames(expected).Should().Equal(PropertyNames(actual));
        JsonEquals(actual.RootElement, expected.RootElement).Should().BeTrue();
    }

    [Fact]
    public void Deserialize_ReadsCanonicalJson_AndIgnoresUnknownFields()
    {
        var message = JsonSerializer.Deserialize<UserFollowedMessage>(CanonicalJson, KafkaJson.Options);

        message.Should().NotBeNull();
        message!.SchemaVersion.Should().Be(1);
        message.EventId.Should().Be("8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c:1b2c3d4e-5f60-4718-92ab-c3d4e5f60718");
        message.FollowerId.Should().Be(FollowerId);
        message.FollowingId.Should().Be(FollowingId);
        message.OccurredAtUtc.Should().Be(OccurredAtUtc);

        const string withExtraField = """
            {
              "schemaVersion": 1,
              "eventId": "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c:1b2c3d4e-5f60-4718-92ab-c3d4e5f60718",
              "occurredAtUtc": "2026-10-12T14:05:00Z",
              "followerId": "1b2c3d4e-5f60-4718-92ab-c3d4e5f60718",
              "followingId": "8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c",
              "displayName": "не входит в факт"
            }
            """;

        var tolerant = JsonSerializer.Deserialize<UserFollowedMessage>(withExtraField, KafkaJson.Options);
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
