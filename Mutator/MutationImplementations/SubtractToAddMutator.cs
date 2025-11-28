using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations;

public class SubtractToAddMutator : BaseMutationImplementation, IMutationImplementation
{
    public SpecifcMutation Mutation => SpecifcMutation.SubtractToAdd;

    public MutationCategory Category => MutationCategory.Arithmetic;

    public SyntaxKind Kind => SyntaxKind.SubtractExpression;

    public Type RequiredNodeType => typeof(BinaryExpressionSyntax);

    public (SyntaxNode mutatedNode, SyntaxAnnotation identififer) Mutate(SyntaxNode node)
    {
        if (node is BinaryExpressionSyntax binaryExp)
        {
            BinaryExpressionSyntax newSyntaxNode = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression,
                        binaryExp.Left,
                        binaryExp.Right);

            return GenerateIdAnnotation(newSyntaxNode);
        }

        throw new MutationException($"Failed to cast syntax node to required type in {nameof(SubtractToAddMutator)}");

    }
}