using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations;

public class LessThanToGreaterThanOrEqualToMutator : BaseMutationImplementation
{
    public override SpecificMutation Mutation => SpecificMutation.LessThanToGreaterThanOrEqualTo;

    public override MutationCategory Category => MutationCategory.Conditional;
    
    public override SyntaxKind Kind => SyntaxKind.LessThanExpression;
    
    public override Type RequiredNodeType => typeof(BinaryExpressionSyntax);
    
    protected override SyntaxNode SpecificMutationImplementation(SyntaxNode node)
    {
        if (node is BinaryExpressionSyntax binaryExp)
        {
            BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.GreaterThanOrEqualExpression,
                        binaryExp.Left,
                        binaryExp.Right);
            return newSyntaxNode;
        }
        throw new MutationException($"Failed to cast syntax node to required type in {nameof(LessThanToGreaterThanOrEqualToMutator)}");
    }
}