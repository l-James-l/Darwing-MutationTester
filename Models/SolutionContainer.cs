using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;

namespace Models;

public class SolutionContainer: ISolutionContainer
{
    public IAnalyzerManager AnalyzerManager { get; init; }

    public AdhocWorkspace Workspace { get; init; }

    public Solution Solution => Workspace.CurrentSolution;

    public List<IProjectContainer> TestProjects { get; private set; } = new List<IProjectContainer>();

    public List<IProjectContainer> SolutionProjects { get; private set; } = new List<IProjectContainer>();

    public List<IProjectContainer> AllProjects { get; } = new List<IProjectContainer>();

    public SolutionContainer(IAnalyzerManager analyzerManager)
    {
        AnalyzerManager = analyzerManager;
        Workspace = analyzerManager.GetWorkspace();

        DiscoverProjects();
    }

    private void DiscoverProjects()
    {
        foreach (Project project in Solution.Projects)
        {
            AllProjects.Add(new ProjectContainer(project));
        }
    }

    public void FindTestProjects(IMutationSettings settings)
    {
        //TODO: In future, should find a way to establish test projects without them needing to be specified.

        TestProjects = AllProjects.Where(x => 
            settings.TestProjectNames.Contains(x.Name) || settings.TestProjectNames.Contains(x.AssemblyName)).ToList();

        // If a project isnt a test project, it is project we can mutate.
        SolutionProjects = AllProjects.Except(TestProjects).ToList();
    }
}

//TODO maybe: Create project container to hold information about each project, such as its path, analyzer and project instance etc.


public interface ISolutionContainer
{
    public List<IProjectContainer> AllProjects { get; }
}
