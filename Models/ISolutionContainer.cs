using Microsoft.CodeAnalysis;

namespace Models;

/// <summary>
/// Mockable container for a loaded solution instance
/// </summary>
public interface ISolutionContainer
{
    /// <summary>
    /// List of all the projects in the solution. Source and test.
    /// </summary>
    public List<IProjectContainer> AllProjects { get; }

    /// <summary>
    /// List of all the projects that are source code projects.
    /// These are what will be mutated
    /// </summary>
    public List<IProjectContainer> SolutionProjects { get; }

    /// <summary>
    /// List of all the test projects in the solutions
    /// </summary>
    public List<IProjectContainer> TestProjects { get; }

    /// <summary>
    /// The loaded solution
    /// </summary>
    public Solution Solution { get; }

    /// <summary>
    /// The workspace. This is how we edit files and apply the changes without altering the actual loaded files
    /// </summary>
    public AdhocWorkspace Workspace { get; }

    /// <summary>
    /// When we apply changes to projects, it creates a new project rather than altering the existing one.
    /// This means that we need to reassign the project properties we precomputed.
    /// </summary>
    void RestoreProjects();
}
