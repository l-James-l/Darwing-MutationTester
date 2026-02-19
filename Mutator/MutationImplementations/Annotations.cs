using Microsoft.CodeAnalysis;

namespace Mutator.MutationImplementations;

public static class Annotations
{
    /// <summary>
    /// The key for the environment variable that determines which mutant is active. This is used to determine which mutant to execute when running the mutated code..
    /// </summary>
    public static string ActiveMutationIndex = "DarwingActiveMutationIndex";

    /// <summary>
    /// Custom annotation used to mark syntax nodes that should not be mutated. This is used to prevent certain mutations from being applied to specific nodes,
    /// such as nodes that are already mutated or nodes that are part of the mutation infrastructure itself.
    /// </summary>
    public static SyntaxAnnotation DontMutateAnnotation = new("DarwingDoNotMutate");
}