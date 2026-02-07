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
    public List<IProjectContainer> TestProjects => AllProjects.Where(x => x.ProjectType is ProjectType.Test).ToList();

    /// <inheritdoc/>
    public List<IProjectContainer> SolutionProjects => AllProjects.Where(x => x.ProjectType is ProjectType.Source).ToList();

    /// <inheritdoc/>
    public List<IProjectContainer> AllProjects { get; private set; } = new List<IProjectContainer>();

    public SolutionContainer(IAnalyzerManager analyzerManager, IMutationSettings settings)
    {
        Workspace = analyzerManager.GetWorkspace();
        
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
            ProjectContainer newProjContainer = new(project, projectAnalyzer);
            AllProjects.Add(newProjContainer);
            
            //Override the determined project type value if the loaded settings specify its type.
            if (settings.SourceCodeProjects.Contains(project.Name) || settings.SourceCodeProjects.Contains(project.AssemblyName))
            {
                newProjContainer.ProjectType = ProjectType.Source;
            }
            else if (settings.TestProjects.Contains(project.Name) || settings.TestProjects.Contains(project.AssemblyName))
            {
                newProjContainer.ProjectType = ProjectType.Test;
            }
            else if (settings.IgnoreProjects.Contains(project.Name) || settings.IgnoreProjects.Contains(project.AssemblyName))
            {
                newProjContainer.ProjectType = ProjectType.Ignore;
            }
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
}
