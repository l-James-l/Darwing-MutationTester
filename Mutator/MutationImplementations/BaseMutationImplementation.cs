using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations;

public abstract class BaseMutationImplementation : IMutationImplementation
{
    private const string NodeIdKey = "DarwingMutatedNodeIdentifier";

    public static SyntaxAnnotation DontMutateAnnotation = new ("DarwingDoNotMutate");

    public abstract SpecifcMutation Mutation { get; }

    public abstract MutationCategory Category { get; }
    
    public abstract SyntaxKind Kind { get; }
    
    public abstract Type RequiredNodeType { get; }

    /// <summary>
    /// Will return a mutated syntax node with a mutation switcher applied to it.
    /// </summary>
    public (SyntaxNode mutatedNode, SyntaxAnnotation identififer) Mutate(SyntaxNode node)
    {
        SyntaxNode mutatedNode = SpecificMutationImplementation(node);
        (mutatedNode, SyntaxAnnotation identififer) = GenerateIdAnnotation(mutatedNode);

        if (mutatedNode is not ExpressionSyntax mutatedExpression)
        {
            throw new MutationException($"Mutation implementation {Mutation} produced a non ExpressionSyntax node.");
        }
        if (node is not ExpressionSyntax originalExpression)
        {
            throw new MutationException($"Mutation implementation {Mutation} received a non ExpressionSyntax node.");
        }

        SyntaxNode mutationSwitcher = BuildMutationSwitcher(originalExpression, mutatedExpression, identififer.Data ?? throw new MutationException("Mutation identifier had no ID."));
        return (mutationSwitcher, identififer);
    }

    /// <summary>
    /// For implementing the specific mutation logic.
    /// For example, changing a + b to a - b.
    /// </summary>
    protected abstract SyntaxNode SpecificMutationImplementation(SyntaxNode node);

    private (SyntaxNode mutatedNode, SyntaxAnnotation identififer) GenerateIdAnnotation(SyntaxNode node)
    {
        var idAnnotation = new SyntaxAnnotation(NodeIdKey, Guid.NewGuid().ToString());
        return (node.WithAdditionalAnnotations(idAnnotation).WithAdditionalAnnotations(DontMutateAnnotation), idAnnotation);
    }

    private SyntaxNode BuildMutationSwitcher(ExpressionSyntax origionalNode, ExpressionSyntax mutatedNode, string id)
    {
        //Should be equivalent to: Environment.GetEnvironmentVariable("DarwingActiveMutaitonIndex")
        InvocationExpressionSyntax activeMutation = SyntaxFactory.InvocationExpression(
            SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxFactory.IdentifierName("Environment"),
                SyntaxFactory.IdentifierName("GetEnvironmentVariable")),
            SyntaxFactory.ArgumentList(
                SyntaxFactory.SeparatedList(
                    new[] {
                            SyntaxFactory.Argument(
                                SyntaxFactory.NameColon("variable"),
                                default,
                                SyntaxFactory.LiteralExpression(
                                    SyntaxKind.StringLiteralExpression,
                                    SyntaxFactory.Literal("DarwingActiveMutaitonIndex")))
                    })
            )
        );

        //Should be equivalent to: Environment.GetEnvironmentVariable("DarwingActiveMutaitonIndex") == id
        BinaryExpressionSyntax condition = 
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression, 
                activeMutation, 
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression, 
                    SyntaxFactory.Literal(id)
                    )
                );

        // should be equivalent to: (Environment.GetEnvironmentVariable("DarwingActiveMutaitonIndex") == id ? mutatedNode : origionalNode)
        ConditionalExpressionSyntax mutationSwitcher = SyntaxFactory.ConditionalExpression(condition, mutatedNode, origionalNode);
        ParenthesizedExpressionSyntax parenthesizedSwticher = SyntaxFactory.ParenthesizedExpression(mutationSwitcher);
        
        return ApplyDontMutateAnnotation(parenthesizedSwticher);
    }

    /// <summary>
    /// To avoid infinite mutation loops, we need to apply a "do not mutate" annotation to all nodes in the subtree.
    /// </summary>
    private SyntaxNode ApplyDontMutateAnnotation(SyntaxNode node)
    {
        node = node.WithAdditionalAnnotations(DontMutateAnnotation);

        Dictionary<SyntaxNode, SyntaxNode> chilrenWithAnnotation = new();
        foreach (SyntaxNode child in node.ChildNodes())
        {
            SyntaxNode childAfterTraversal = ApplyDontMutateAnnotation(child);
            chilrenWithAnnotation.Add(child, childAfterTraversal);
        }

        //Replace all the nodes children with thier annotated counterparts. 
        node = node.ReplaceNodes(node.ChildNodes(), (x, _) =>
        {
            if (chilrenWithAnnotation.TryGetValue(x, out var mutated))
            {
                return mutated;
            }
            return x;
        });

        return node;
    }
}