using System.Text.RegularExpressions;
using FluentAssertions;

namespace Venture.ArchitectureTests;

public class DatabaseOwnershipTests
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedDatabases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Users"] = "venture_users",
            ["Posts"] = "venture_posts",
            ["Social"] = "venture_social",
            ["Achievements"] = "venture_achievements",
            ["Notifications"] = "venture_notifications"
        };

    [Fact]
    public void EachServiceConnectionStringNamesOnlyItsOwnDatabase()
    {
        foreach (var (service, database) in ExpectedDatabases)
        {
            var serviceDirectory = Path.Combine(RepositoryLayout.ServicesRoot, service);
            Directory.Exists(serviceDirectory).Should().BeTrue($"{service} должен лежать в services");

            var settings = Directory.GetFiles(serviceDirectory, "appsettings*.json", SearchOption.AllDirectories);
            settings.Should().NotBeEmpty($"{service} должен содержать appsettings");

            var names = settings
                .SelectMany(file => DatabaseNames(File.ReadAllText(file)))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            names.Should().BeEquivalentTo([database], $"{service} пишет только в {database}");
        }
    }

    [Fact]
    public void GatewayDoesNotOwnADatabase()
    {
        var gateway = Path.Combine(RepositoryLayout.ServicesRoot, "Gateway");
        Directory.Exists(gateway).Should().BeTrue();

        var settings = Directory.GetFiles(gateway, "appsettings*.json", SearchOption.AllDirectories);
        settings.Should().NotBeEmpty();

        var names = settings.SelectMany(file => DatabaseNames(File.ReadAllText(file))).ToArray();
        names.Should().BeEmpty();
    }

    private static IEnumerable<string> DatabaseNames(string json) =>
        Regex.Matches(json, @"Database=([A-Za-z0-9_]+)")
            .Select(match => match.Groups[1].Value);
}
