using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations;

public class SubtractToAddMutator : BaseMutationImplementation
{
    public override SpecifcMutation Mutation => SpecifcMutation.SubtractToAdd;

    public override MutationCategory Category => MutationCategory.Arithmetic;

    public override SyntaxKind Kind => SyntaxKind.SubtractExpression;

    public override Type RequiredNodeType => typeof(BinaryExpressionSyntax);

    protected override SyntaxNode SpecificMutationImplementation(SyntaxNode node)
    {
        if (node is BinaryExpressionSyntax binaryExp)
        {
            BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression,
                        binaryExp.Left,
                        binaryExp.Right);

            return newSyntaxNode;
        }

        throw new MutationException($"Failed to cast syntax node to required type in {nameof(SubtractToAddMutator)}");

    }
}