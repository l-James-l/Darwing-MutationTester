using Microsoft.CodeAnalysis;
using Serilog;

namespace Models;


/// <summary>
/// Storing project information in a container allows tests to mock project info without loading a real project.
/// </summary>
public class ProjectContainer : IProjectContainer
{
    private Project _project;

    public ProjectContainer(Project project)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(project.FilePath);

        _project = project;
        CsprojFilePath = project.FilePath;
        DirectoryPath = Path.GetDirectoryName(_project.FilePath) ??
            CsprojFilePath.Remove(CsprojFilePath.Count() - _project.Name.Count());
        
        //TODO: maybe throwing here isnt the most robust solution.
        DllFilePath = project.OutputFilePath ?? throw new Exception($"Could not establish the output file path for {Name}");
    }

    public string CsprojFilePath { get; }

    public string DirectoryPath { get; }

    public string Name => _project.Name;

    public string AssemblyName => _project.AssemblyName;

    public string DllFilePath { get; }

    public Dictionary<DocumentId, SyntaxTree> UnMutatedSyntaxTrees { get; } = new();

    public Dictionary<string, DocumentId> DocumentsByPath { get; } = new();

    public Compilation? GetCompilation() => _project.GetCompilationAsync().GetAwaiter().GetResult();

    public void UpdateFromMutatedProject(Project proj)
    {
        if (proj.Id == _project.Id)
        {
            _project = proj;
        }
        else
        {
            Log.Error("Could not update project {project} as IDS did not match.", _project.Name);
        }
    }
}
