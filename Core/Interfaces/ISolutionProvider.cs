using Models;

namespace Core.Interfaces;

public interface ISolutionProvider
{
    public bool IsAvailable { get; }

    ISolutionContainer SolutionContiner { get; }

}
