namespace Mutator;


/// <summary>
/// Exception for when a mutation implementation fails to created a mutated node.
/// </summary>
public class MutationException : Exception
{
    public MutationException(string msg) : base(msg) { }
}