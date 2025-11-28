using Microsoft.CodeAnalysis;

namespace Mutator.MutationImplementations;

public abstract class BaseMutationImplementation
{
    private const string NodeIdKey = "DarwingMutatedNodeIdentifier";

    protected (SyntaxNode mutatedNode, SyntaxAnnotation identififer) GenerateIdAnnotation(SyntaxNode node)
    {
        var idAnnotation = new SyntaxAnnotation(NodeIdKey, Guid.NewGuid().ToString());
        return (node.WithAdditionalAnnotations(idAnnotation), idAnnotation);
    }
}