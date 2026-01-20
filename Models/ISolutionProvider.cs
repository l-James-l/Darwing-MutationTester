namespace Models;

public interface ISolutionProvider
{
    /// <summary>
    /// When a new solution is loaded, this method is called to update the provider.
    /// </summary>
    /// <param name="solution"></param>
    public void NewSolution(ISolutionContainer solution);

    /// <summary>
    /// Has a solution been loaded?
    /// </summary>
    public bool IsAvailable { get; }

    /// <summary>
    /// The currently loaded solution.
    /// Not nullable because if a solution hasn't been loaded, IsAvailable will be false,
    /// and attempting to access this property will throw an exception.
    /// </summary>
    ISolutionContainer SolutionContainer { get; }

}
