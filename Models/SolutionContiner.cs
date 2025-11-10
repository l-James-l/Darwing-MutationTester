using Buildalyzer;
using Buildalyzer.Workspaces;
using Microsoft.CodeAnalysis;

namespace Core;

public class SolutionContiner : ISolutionContainer
{
    public IAnalyzerManager AnalyzerManager { get; init; }

    public AdhocWorkspace Workspace { get; init; }

    public Solution Solution => Workspace.CurrentSolution;

    public List<Project> TestProjects => throw new NotImplementedException("Need to find a way to establish test projects.");

    public List<Project> SolutionProjects => throw new NotImplementedException("Need to find a way to distinguish non test projects.");

    public List<Project> AllProjects => Solution.Projects.ToList();

    public SolutionContiner(IAnalyzerManager analyzerManager)
    {
        AnalyzerManager = analyzerManager;
        Workspace = analyzerManager.GetWorkspace();
    }
}

//TODO maybe: Create project container to hold information about each project, such as its path, analyzer and project instance etc.
