using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations;

public class AddToSubtractMutator : BaseMutationImplementation
{
    public override SpecifcMutation Mutation => SpecifcMutation.AddToSubtract;

    public override MutationCategory Category => MutationCategory.Arithmetic;

    public override SyntaxKind Kind => SyntaxKind.AddExpression;

    public override Type RequiredNodeType => typeof(BinaryExpressionSyntax);


    protected override SyntaxNode SpecificMutationImplementation(SyntaxNode node)
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
