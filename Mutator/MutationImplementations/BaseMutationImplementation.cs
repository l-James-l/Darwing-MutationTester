using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations;

public abstract class BaseMutationImplementation : IMutationImplementation
{
    private const string NodeIdKey = "DarwingMutatedNodeIdentifier";
    
    public static string ActiveMutationIndex = "DarwingActiveMutationIndex";
    public static SyntaxAnnotation DontMutateAnnotation = new ("DarwingDoNotMutate");

    public abstract SpecifcMutation Mutation { get; }

    public abstract MutationCategory Category { get; }
    
    public abstract SyntaxKind Kind { get; }
    
    public abstract Type RequiredNodeType { get; }

    /// <summary>
    /// Will return a mutated syntax node with a mutation switcher applied to it.
    /// </summary>
    public (SyntaxNode mutationSwitcher, SyntaxAnnotation identififer, SyntaxNode mutatedNode) Mutate(SyntaxNode node)
    {
        SyntaxNode mutatedNode = SpecificMutationImplementation(node);
        (mutatedNode, SyntaxAnnotation identifier) = GenerateIdAnnotation(mutatedNode);
        mutatedNode = mutatedNode.NormalizeWhitespace();
        if (mutatedNode is not ExpressionSyntax mutatedExpression)
        {
            throw new MutationException($"Mutation implementation {Mutation} produced a non ExpressionSyntax node.");
        }
        if (node is not ExpressionSyntax originalExpression)
        {
            throw new MutationException($"Mutation implementation {Mutation} received a non ExpressionSyntax node.");
        }

        SyntaxNode mutationSwitcher = BuildMutationSwitcher(originalExpression, mutatedExpression, identifier.Data ?? throw new MutationException("Mutation identifier had no ID."));
        return (mutationSwitcher, identifier, mutatedNode);
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

    private SyntaxNode BuildMutationSwitcher(ExpressionSyntax originalNode, ExpressionSyntax mutatedNode, string id)
    {
        //Should be equivalent to: Environment.GetEnvironmentVariable("DarwingActiveMutationIndex")
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
                                    SyntaxFactory.Literal(ActiveMutationIndex)))
                    })
            )
        );

        //Should be equivalent to: Environment.GetEnvironmentVariable("DarwingActiveMutationIndex") == id
        BinaryExpressionSyntax condition = 
            SyntaxFactory.BinaryExpression(
                SyntaxKind.EqualsExpression, 
                activeMutation, 
                SyntaxFactory.LiteralExpression(
                    SyntaxKind.StringLiteralExpression, 
                    SyntaxFactory.Literal(id)
                    )
                );

        condition = ApplyDontMutateAnnotation(condition) as BinaryExpressionSyntax ?? throw new MutationException("Applying dont mutate annotation changed node type.");
        mutatedNode = ApplyDontMutateAnnotation(mutatedNode) as ExpressionSyntax ?? throw new MutationException("Applying dont mutate annotation changed node type.");
        originalNode = originalNode.WithAdditionalAnnotations(DontMutateAnnotation); // original node only needs the top level dont mutate annotation.

        // should be equivalent to: (Environment.GetEnvironmentVariable("DarwingActiveMutationIndex") == id ? mutatedNode : originalNode)
        ConditionalExpressionSyntax mutationSwitcher = SyntaxFactory.ConditionalExpression(condition, mutatedNode, originalNode).WithAdditionalAnnotations(DontMutateAnnotation);
        ParenthesizedExpressionSyntax parenthesizedSwitcher = SyntaxFactory.ParenthesizedExpression(mutationSwitcher).WithAdditionalAnnotations(DontMutateAnnotation);
        
        return parenthesizedSwitcher.NormalizeWhitespace();
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