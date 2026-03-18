using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations.Relational;

public class GreaterThanOrEqualToLessThan : BaseMutationImplementation
{
    public override SpecificMutation Mutation => SpecificMutation.GreaterThanOrEqualToLessThan;

    public override MutationCategory Category => MutationCategory.Relational;

    public override SyntaxKind Kind => SyntaxKind.GreaterThanOrEqualExpression;

    public override Type RequiredNodeType => typeof(BinaryExpressionSyntax);

    protected override SyntaxNode SpecificMutationImplementation(SyntaxNode node)
    {
        if (node is BinaryExpressionSyntax binaryExp)
        {
            BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.LessThanExpression,
                        binaryExp.Left,
                        binaryExp.Right);

            return newSyntaxNode;
        }

        throw new MutationException($"Failed to cast syntax node to required type in {nameof(GreaterThanOrEqualToLessThan)}");
    }
}
