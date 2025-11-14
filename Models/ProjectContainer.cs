using Microsoft.CodeAnalysis;

namespace Models;


/// <summary>
/// Storing project information in a container allows tests to mock project info without loading a real project.
/// </summary>
public class ProjectContainer : IProjectContainer
{
    private readonly Project _project;

    public ProjectContainer(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(project.FilePath);

        _project = project;
    }

    public string CsprojFilePath => _project.FilePath!;

    public string Name => _project.Name;

    public string AssemblyName => _project.AssemblyName;
}


public interface IProjectContainer
{
    string Name { get; }

    string CsprojFilePath { get; }

    string AssemblyName { get; }
}
