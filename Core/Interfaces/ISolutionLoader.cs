namespace Core.Interfaces;

public interface ISolutionLoader
{
    /// <summary>
    /// Loads the solution located at the provided path.
    /// </summary>
    public void Load(string solutionPath);
}
