using Microsoft.CodeAnalysis;
using Models.Enums;

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
    /// The path to the directory containing the solution file.
    /// </summary>
    public string DirectoryPath { get; }

    /// <summary>
    /// When we apply changes to projects, it creates a new project rather than altering the existing one.
    /// This means that we need to reassign the project properties we precomputed.
    /// </summary>
    void RestoreProjects();

    /// <summary>
    /// Find the given file if it exists in any project.
    /// Can optionally filter by project type
    /// </summary>
    /// <param name="path">Full path for file</param>
    /// <param name="inType">The type of project the file should be found in</param>
    public SourceCodeFileContainer? FindFile(string path, ProjectType? inType = null);

    /// <summary>
    /// Find the given file if it exists in any project.
    /// Can optionally filter by project type
    /// </summary>
    /// <param name="id">The document ID</param>
    /// <param name="inType">The type of project the file should be found in</param>
    public SourceCodeFileContainer? FindFile(DocumentId id, ProjectType? inType = null);
}

