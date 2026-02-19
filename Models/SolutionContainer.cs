using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Models.Enums;
using Serilog;

namespace Models;

public class SolutionContainer: ISolutionContainer
{
    /// <inheritdoc/>
    public AdhocWorkspace Workspace { get; init; }

    /// <inheritdoc/>
    public Solution Solution => Workspace.CurrentSolution;

    /// <inheritdoc/>
    public string DirectoryPath { get; private set; }

    /// <inheritdoc/>
    public List<IProjectContainer> TestProjects => AllProjects.Where(x => x.ProjectType is ProjectType.Test).ToList();

    /// <inheritdoc/>
    public List<IProjectContainer> SolutionProjects => AllProjects.Where(x => x.ProjectType is ProjectType.Source).ToList();

    /// <inheritdoc/>
    public List<IProjectContainer> AllProjects { get; private set; } = new List<IProjectContainer>();

    public SolutionContainer(IAnalyzerManager analyzerManager, IMutationSettings settings)
    {
        Workspace = analyzerManager.GetWorkspace();

        DirectoryPath = Path.GetDirectoryName(analyzerManager.SolutionFilePath) ??
            analyzerManager.SolutionFilePath.Remove(analyzerManager.SolutionFilePath.Length - Path.GetFileName(analyzerManager.SolutionFilePath).Length);

        DiscoverProjects(analyzerManager, settings);
    }

    private void DiscoverProjects(IAnalyzerManager analyzerManager, IMutationSettings settings)
    {
        foreach (Project project in Solution.Projects)
        {
            if (project.FilePath is null)
            {
                Log.Warning("Project: {proj}, has no file path and will be ignored.");
                continue;
            }
            IProjectAnalyzer projectAnalyzer = analyzerManager.GetProject(project.FilePath);
            ProjectContainer newProjContainer = new(project, projectAnalyzer, settings);
            AllProjects.Add(newProjContainer);
        }
    }

    /// <inheritdoc/>
    public void RestoreProjects()
    {
        foreach (Project proj in Solution.Projects)
        {
            AllProjects.FirstOrDefault(x => x.Name == proj.Name)?.UpdateFromMutatedProject(proj);
        }
    }

    /// <inheritdoc/>
    public SourceCodeFileContainer? FindFile(string path, ProjectType? inType=null)
    {
        foreach (IProjectContainer proj in AllProjects.Where(x => inType is null || x.ProjectType == inType))
        {
            if (proj.FileCollection.TryGetValue(path, out SourceCodeFileContainer? file))
            {
                return file;
            }
        }
        return null;
    }
    
    /// <inheritdoc/>
    public SourceCodeFileContainer? FindFile(DocumentId id, ProjectType? inType=null)
    {
        foreach (IProjectContainer proj in AllProjects.Where(x => inType is null || x.ProjectType == inType))
        {
            if (proj.FileCollection.TryGetValue(id, out SourceCodeFileContainer? file))
            {
                return file;
            }
        }
        return null;
    }
}
