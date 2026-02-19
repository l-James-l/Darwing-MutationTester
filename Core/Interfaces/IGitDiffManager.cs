namespace Core.Interfaces;

public interface IGitDiffManager
{
    /// <summary>
    /// Call when a solution is first loaded. This will load the git repo and then establish the diff
    /// to the default branch.
    /// </summary>
    void InitialGitDiff();


    /// <summary>
    /// Will set the files/lines to mutate in the solution container.
    /// </summary>
    void EstablishDiff(string compareBranch);

    /// <summary>
    /// List of available branch names in the git repository. If no repository is loaded, or the repository fails to load, this will be an empty list.
    /// </summary>
    List<string> Branches { get; }

    /// <summary>
    /// The last branch that a diff was successfully established to. This will be null if no diff has been established, or if the repository failed to load.
    /// </summary>
    public string? LastSelectedBranch { get; }

}