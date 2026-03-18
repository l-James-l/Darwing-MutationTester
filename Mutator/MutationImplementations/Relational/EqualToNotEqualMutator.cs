using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations.Relational;

public class EqualToNotEqualMutator : BaseMutationImplementation
{
    public override SpecificMutation Mutation => SpecificMutation.EqualToNotEqual;

    public override MutationCategory Category => MutationCategory.Logical;
    
    public override SyntaxKind Kind => SyntaxKind.EqualsExpression;
    
    public override Type RequiredNodeType => typeof(BinaryExpressionSyntax);
    
    protected override SyntaxNode SpecificMutationImplementation(SyntaxNode node)
    {
        if (node is BinaryExpressionSyntax binaryExp)
        {
            BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression,
                        binaryExp.Left,
                        binaryExp.Right);
            return newSyntaxNode;
        }
        throw new MutationException($"Failed to cast syntax node to required type in {nameof(EqualToNotEqualMutator)}");
    }
}
