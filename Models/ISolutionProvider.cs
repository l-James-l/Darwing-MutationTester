namespace Models;

public interface ISolutionProvider
{
    public bool IsAvailable { get; }

    ISolutionContainer SolutionContainer { get; }

}
