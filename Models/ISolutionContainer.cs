using Microsoft.CodeAnalysis;

namespace Models;

public interface ISolutionContainer
{
    public List<IProjectContainer> AllProjects { get; }

    public List<IProjectContainer> SolutionProjects { get; }

    public Solution Solution { get; }

    public AdhocWorkspace Workspace { get; }

    void RestoreProjects();
}
