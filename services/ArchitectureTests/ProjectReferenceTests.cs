using System.Xml.Linq;
using FluentAssertions;

namespace Venture.ArchitectureTests;

public class ProjectReferenceTests
{
    [Fact]
    public void ProjectReferencesStayInsideTheSameService()
    {
        var projects = Directory.GetFiles(RepositoryLayout.ServicesRoot, "*.csproj", SearchOption.AllDirectories);
        projects.Should().NotBeEmpty("проверка должна видеть реальные проекты, а не пустой каталог");

        var violations = new List<string>();

        foreach (var project in projects)
        {
            var owner = RepositoryLayout.ServiceName(project);
            var document = XDocument.Load(project);

            foreach (var reference in document.Descendants().Where(element => element.Name.LocalName == "ProjectReference"))
            {
                var include = reference.Attribute("Include")?.Value;
                if (string.IsNullOrWhiteSpace(include))
                {
                    violations.Add($"{project}: ProjectReference без Include");
                    continue;
                }

                var referencedPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(project)!, include));

                if (!File.Exists(referencedPath))
                {
                    violations.Add($"{project} ссылается на отсутствующий проект {include}");
                    continue;
                }

                var referencedService = RepositoryLayout.ServiceName(referencedPath);
                if (!string.Equals(owner, referencedService, StringComparison.OrdinalIgnoreCase))
                {
                    violations.Add(
                        $"{Path.GetRelativePath(RepositoryLayout.Root, project)} ({owner}) -> {include} ({referencedService})");
                }
            }
        }

        violations.Should().BeEmpty();
    }
}
