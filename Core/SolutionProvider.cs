using Models;

namespace Core;

public class SolutionProvider : ISolutionProvider
{
    /// <inheritdoc/>
    public ISolutionContainer SolutionContainer => _solutionContainer ?? throw new InvalidOperationException("Attempted to retrieve a solution before one has been loaded.");
    private ISolutionContainer? _solutionContainer;

    /// <inheritdoc/>
    public bool IsAvailable => _solutionContainer != null;

    /// <inheritdoc/>
    public void NewSolution(ISolutionContainer solution)
    {
        _solutionContainer = solution;
    }
}
