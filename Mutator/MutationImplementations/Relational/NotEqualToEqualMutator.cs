using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations.Relational;

public class NotEqualToEqualMutator : BaseMutationImplementation
{
    public override SpecificMutation Mutation => SpecificMutation.NotEqualToEqual;
   
    public override MutationCategory Category => MutationCategory.Logical;
    
    public override SyntaxKind Kind => SyntaxKind.NotEqualsExpression;
    
    public override Type RequiredNodeType => typeof(BinaryExpressionSyntax);
    
    protected override SyntaxNode SpecificMutationImplementation(SyntaxNode node)
    {
        if (node is BinaryExpressionSyntax binaryExp)
        {
            BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                        binaryExp.Left,
                        binaryExp.Right);
            return newSyntaxNode;
        }
        throw new MutationException($"Failed to cast syntax node to required type in {nameof(NotEqualToEqualMutator)}");
    }
}