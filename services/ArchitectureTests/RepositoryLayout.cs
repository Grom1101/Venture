namespace Venture.ArchitectureTests;

internal static class RepositoryLayout
{
    public static string Root { get; } = FindRoot();

    public static string ServicesRoot => Path.Combine(Root, "services");

    public static IReadOnlyList<string> ServiceDirectories()
    {
        var directories = Directory.GetDirectories(ServicesRoot);
        if (directories.Length == 0)
            throw new InvalidOperationException($"В {ServicesRoot} нет каталогов сервисов.");

        return directories;
    }

    public static string ServiceName(string path)
    {
        var relative = Path.GetRelativePath(ServicesRoot, path);
        if (relative.StartsWith("..", StringComparison.Ordinal))
            throw new InvalidOperationException($"{path} лежит вне services.");

        var separator = relative.IndexOfAny(['\\', '/']);
        var name = separator < 0 ? relative : relative[..separator];

        if (string.IsNullOrWhiteSpace(name))
            throw new InvalidOperationException($"Не удалось определить сервис для {path}.");

        return name;
    }

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            var marker = Path.Combine(directory.FullName, "review", "target-architecture.md");
            var services = Path.Combine(directory.FullName, "services", "Posts", "Posts.slnx");

            if (File.Exists(marker) && File.Exists(services))
                return directory.FullName;

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Корень репозитория Venture не найден от каталога теста.");
    }
}
