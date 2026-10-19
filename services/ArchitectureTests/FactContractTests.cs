using FluentAssertions;

namespace Venture.ArchitectureTests;

public class FactContractTests
{
    [Fact]
    public void PostPublishedJsonIsTheSameInPublisherAndConsumer()
    {
        var posts = Read("Posts", "Posts.Application.Tests", "Messaging", "PostPublishedMessageTests.cs");
        var achievements = Read("Achievements", "Achievements.IntegrationTests", "Messaging", "FactContractTests.cs");

        foreach (var line in new[]
                 {
                     "\"schemaVersion\": 1",
                     "\"eventId\": \"2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11\"",
                     "\"occurredAtUtc\": \"2026-10-12T14:00:00Z\"",
                     "\"postId\": \"2c1f5b0e-7a0d-4c1a-9f2e-6b8d4a1c0e11\"",
                     "\"authorId\": \"8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c\""
                 })
        {
            posts.Should().Contain(line);
            achievements.Should().Contain(line);
        }
    }

    [Fact]
    public void UserFollowedJsonIsTheSameInPublisherAndConsumer()
    {
        var social = Read("Social", "Social.Application.Tests", "Messaging", "UserFollowedMessageTests.cs");
        var achievements = Read("Achievements", "Achievements.IntegrationTests", "Messaging", "FactContractTests.cs");

        foreach (var line in new[]
                 {
                     "\"schemaVersion\": 1",
                     "\"eventId\": \"8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c:1b2c3d4e-5f60-4718-92ab-c3d4e5f60718\"",
                     "\"occurredAtUtc\": \"2026-10-12T14:05:00Z\"",
                     "\"followerId\": \"1b2c3d4e-5f60-4718-92ab-c3d4e5f60718\"",
                     "\"followingId\": \"8a0d1c2e-4b6f-4a11-9c33-1d2e3f4a5b6c\""
                 })
        {
            social.Should().Contain(line);
            achievements.Should().Contain(line);
        }
    }

    private static string Read(string service, string project, string folder, string file)
    {
        var path = Path.Combine(RepositoryLayout.ServicesRoot, service, project, folder, file);
        File.Exists(path).Should().BeTrue($"контракт должен лежать в {path}");
        return File.ReadAllText(path);
    }
}
