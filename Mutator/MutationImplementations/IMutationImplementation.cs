using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models.Enums;

namespace Mutator.MutationImplementations;

public interface IMutationImplementation
{
    SpecifcMutation Mutation { get; }

    MutationCategory Category { get; }

    SyntaxKind Kind { get; }

    Type RequiredNodeType { get; }

    (SyntaxNode mutatedNode, SyntaxAnnotation identififer) Mutate(SyntaxNode node);   
}
