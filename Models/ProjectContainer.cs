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
        CsprojFilePath = project.FilePath;
        DirectoryPath = Path.GetDirectoryName(_project.FilePath) ??
            CsprojFilePath.Remove(CsprojFilePath.Count() - _project.Name.Count());
    }

    public string CsprojFilePath { get; }

    public string DirectoryPath { get; }

    public string Name => _project.Name;

    public string AssemblyName => _project.AssemblyName;

    public List<SyntaxTree> SyntaxTrees { get; } = new();
}


public interface IProjectContainer
{
    string Name { get; }

    string CsprojFilePath { get; }

    public string DirectoryPath { get; }

    string AssemblyName { get; }

    public List<SyntaxTree> SyntaxTrees { get; }

}
