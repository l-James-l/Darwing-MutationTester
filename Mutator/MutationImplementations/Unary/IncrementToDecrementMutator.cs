using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations.Unary;

public class IncrementToDecrementMutator : BaseMutationImplementation
{
    public IncrementToDecrementMutator()
    {
        WrapInDiscardedMethod = true;
    }

    public override SpecificMutation Mutation => SpecificMutation.IncrementToDecrement;

    public override MutationCategory Category => MutationCategory.Unary;
    
    public override SyntaxKind Kind => SyntaxKind.PostIncrementExpression;
    
    public override Type RequiredNodeType => typeof(PostfixUnaryExpressionSyntax);

    protected override SyntaxNode SpecificMutationImplementation(SyntaxNode node)
    {
        if (node is PostfixUnaryExpressionSyntax unaryExp)
        {
            PostfixUnaryExpressionSyntax newSyntaxNode = SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostDecrementExpression,
                        unaryExp.Operand);
            return newSyntaxNode;
        }
        throw new MutationException($"Failed to cast syntax node to required type in {nameof(IncrementToDecrementMutator)}");
    }
}
