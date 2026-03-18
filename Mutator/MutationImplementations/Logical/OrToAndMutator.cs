using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations.Logical;

public class OrToAndMutator : BaseMutationImplementation
{
    public override SpecificMutation Mutation => SpecificMutation.OrToAnd;

    public override MutationCategory Category => MutationCategory.Logical;

    public override SyntaxKind Kind => SyntaxKind.LogicalOrExpression;

    public override Type RequiredNodeType => typeof(BinaryExpressionSyntax);

    protected override SyntaxNode SpecificMutationImplementation(SyntaxNode node)
    {
        if (node is  BinaryExpressionSyntax binaryExpression && binaryExpression.IsKind(Kind))
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, binaryExpression.Left, binaryExpression.Right);
        }

        throw new MutationException($"Failed to cast syntax node to required type in {nameof(AndToOrMutator)}");
    }
}
