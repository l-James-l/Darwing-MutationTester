using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations;

public class AddToSubtractMutator : IMutationImplementation
{
    public SpecifcMutation Mutation => SpecifcMutation.AddToSubtract;

    public MutationCategory Category => MutationCategory.Arithmetic;

    public SyntaxKind Kind => SyntaxKind.AddExpression;

    public Type RequiredNodeType => typeof(BinaryExpressionSyntax);

    public SyntaxNode Mutate(SyntaxNode node)
    {
        if (node is BinaryExpressionSyntax binaryExp)
        {
            BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression,
                        binaryExp.Left,
                        binaryExp.Right);

            return newSyntaxNode;
        }

        throw new MutationException($"Failed to cast syntax node to required type in {nameof(AddToSubtractMutator)}");

    }
}
