using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations.Unary;

public class DecrementToIncrementMutator : BaseMutationImplementation
{
    public DecrementToIncrementMutator()
    {
        WrapInDiscardedMethod = true;
    }

    public override SpecificMutation Mutation => SpecificMutation.DecrementToIncrement;
    
    public override MutationCategory Category => MutationCategory.Unary;
    
    public override SyntaxKind Kind => SyntaxKind.PostDecrementExpression;
    
    public override Type RequiredNodeType => typeof(PostfixUnaryExpressionSyntax);
    
    protected override SyntaxNode SpecificMutationImplementation(SyntaxNode node)
    {
        if (node is PostfixUnaryExpressionSyntax unaryExp)
        {
            PostfixUnaryExpressionSyntax newSyntaxNode = SyntaxFactory.PostfixUnaryExpression(SyntaxKind.PostIncrementExpression,
                        unaryExp.Operand);
            return newSyntaxNode;
        }
        throw new MutationException($"Failed to cast syntax node to required type in {nameof(DecrementToIncrementMutator)}");
    }
}