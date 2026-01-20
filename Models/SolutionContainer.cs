using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;
using Serilog;

namespace Models;

public class SolutionContainer: ISolutionContainer
{
    public AdhocWorkspace Workspace { get; init; }

    public Solution Solution => Workspace.CurrentSolution;

    public List<IProjectContainer> TestProjects { get; private set; } = new List<IProjectContainer>();

    public List<IProjectContainer> SolutionProjects { get; private set; } = new List<IProjectContainer>();

    public List<IProjectContainer> AllProjects { get; private set; } = new List<IProjectContainer>();

    public SolutionContainer(IAnalyzerManager analyzerManager, IMutationSettings settings)
    {
        Workspace = analyzerManager.GetWorkspace();
        
        DiscoverProjects(analyzerManager);
        FindTestProjects(settings);
    }

    private void DiscoverProjects(IAnalyzerManager analyzerManager)
    {
        foreach (Project project in Solution.Projects)
        {
            if (project.FilePath is null)
            {
                Log.Warning("Project: {proj}, has no file path and will be ignored.");
                continue;
            }
            IProjectAnalyzer projectAnalyzer = analyzerManager.GetProject(project.FilePath);
            AllProjects.Add(new ProjectContainer(project, projectAnalyzer));
        }
    }

    private void FindTestProjects(IMutationSettings settings)
    {
        TestProjects = AllProjects.Where(x => 
            x.IsTestProject || 
            settings.TestProjectNames.Contains(x.Name) ||
            settings.TestProjectNames.Contains(x.AssemblyName)).ToList();

        // If a project isn't a test project, it is project we can mutate.
        SolutionProjects = AllProjects.Except(TestProjects).ToList();
    }

    public void RestoreProjects()
    {
        foreach (Project proj in Solution.Projects)
        {
            AllProjects.FirstOrDefault(x => x.Name == proj.Name)?.UpdateFromMutatedProject(proj);
        }
    }
}
