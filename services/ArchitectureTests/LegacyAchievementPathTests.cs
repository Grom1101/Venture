using FluentAssertions;

namespace Venture.ArchitectureTests;

public class LegacyAchievementPathTests
{
    private static readonly string[] Forbidden =
    [
        "enum AchievementType",
        "GrantFirstPostAchievement",
        "GrantFirstFollowerAchievement",
        "venture.achievements.first-post",
        "venture.achievements.first-follower"
    ];

    [Fact]
    public void UsersPostsAndSocialDoNotKeepTheOldAchievementGrantPath()
    {
        var roots = new[] { "Users", "Posts", "Social" };
        var hits = new List<string>();

        foreach (var service in roots)
        {
            var root = Path.Combine(RepositoryLayout.ServicesRoot, service);
            Directory.Exists(root).Should().BeTrue();

            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                if (IsGeneratedOrBinary(file))
                    continue;

                var text = File.ReadAllText(file);

                foreach (var forbidden in Forbidden)
                {
                    if (text.Contains(forbidden, StringComparison.Ordinal))
                    {
                        hits.Add($"{Path.GetRelativePath(RepositoryLayout.Root, file)}: {forbidden}");
                    }
                }
            }
        }

        hits.Should().BeEmpty();
    }

    private static bool IsGeneratedOrBinary(string file)
    {
        var relative = Path.GetRelativePath(RepositoryLayout.ServicesRoot, file);
        var parts = relative.Split(['\\', '/']);

        if (parts.Any(part => part is "bin" or "obj"))
            return true;

        var extension = Path.GetExtension(file);
        return extension is not (".cs" or ".csproj" or ".json" or ".js");
    }
}
