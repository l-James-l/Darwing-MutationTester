using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations.Arithmetic;

public class DivideToMultiplyMutator : BaseMutationImplementation
{
    public override SpecificMutation Mutation => SpecificMutation.DivideToMultiply;

    public override MutationCategory Category => MutationCategory.Arithmetic;

    public override SyntaxKind Kind => SyntaxKind.DivideExpression;

    public override Type RequiredNodeType => typeof(BinaryExpressionSyntax);

    protected override SyntaxNode SpecificMutationImplementation(SyntaxNode node)
    {
        if (node is BinaryExpressionSyntax binaryExp && binaryExp.IsKind(Kind))
        {
            return SyntaxFactory.BinaryExpression(SyntaxKind.MultiplyExpression, binaryExp.Left, binaryExp.Right);
        }

        throw new MutationException($"Failed to cast syntax node to required type in {nameof(DivideToMultiplyMutator)}");

    }
}
