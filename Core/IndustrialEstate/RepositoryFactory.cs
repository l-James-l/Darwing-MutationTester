using LibGit2Sharp;

namespace Core.IndustrialEstate;


/// <inheritdoc/>
public class RepositoryFactory : IRepositoryFactory
{
    /// <inheritdoc/>
    public IRepository Get(string path)
    {
        return new Repository(path);
    }
}

/// <summary>
/// Factory pattern to get the interface for the concreate type of a GIT repository
/// </summary>
public interface IRepositoryFactory
{
    /// <summary>
    /// Get repository
    /// </summary>
    /// <param name="path">Path to .git folder in a local repo</param>
    /// <returns>LibGit2Sharp repo</returns>
    IRepository Get(string path);
}