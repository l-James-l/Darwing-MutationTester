using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;

namespace Mutator.MutationImplementations;

public abstract class BaseMutationImplementation : IMutationImplementation
{
    private const string _nodeIdKey = "DarwingMutatedNodeIdentifier";

    public abstract SpecificMutation Mutation { get; }

    public abstract MutationCategory Category { get; }
    
    public abstract SyntaxKind Kind { get; }
    
    public abstract Type RequiredNodeType { get; }

    /// <summary>
    /// For some mutation types, replacing the node with the standard mutation switcher introduces build errors.
    /// If true, this will add an anonymous discard method around it
    /// _ = (SyntaxSwitcher)
    /// </summary>
    protected virtual bool WrapInDiscardedMethod { get; set; } = false;

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
        var idAnnotation = new SyntaxAnnotation(_nodeIdKey, Guid.NewGuid().ToString());
        return (node.WithAdditionalAnnotations(idAnnotation).WithAdditionalAnnotations(Annotations.DontMutateAnnotation), idAnnotation);
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
                                    SyntaxFactory.Literal(Annotations.ActiveMutationIndex)))
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
        originalNode = originalNode.WithAdditionalAnnotations(Annotations.DontMutateAnnotation); // original node only needs the top level dont mutate annotation.

        // should be equivalent to: (Environment.GetEnvironmentVariable("DarwingActiveMutationIndex") == id ? mutatedNode : originalNode)
        ExpressionSyntax mutationSwitcher = SyntaxFactory.ConditionalExpression(condition, mutatedNode, originalNode).WithAdditionalAnnotations(Annotations.DontMutateAnnotation);
        ExpressionSyntax parenthesizedSwitcher = SyntaxFactory.ParenthesizedExpression(mutationSwitcher).WithAdditionalAnnotations(Annotations.DontMutateAnnotation);
        
        if (WrapInDiscardedMethod)
        {
            parenthesizedSwitcher = WrapInDiscardMethod(parenthesizedSwitcher).WithAdditionalAnnotations(Annotations.DontMutateAnnotation);
        }

        return parenthesizedSwitcher.NormalizeWhitespace();
    }

    /// <summary>
    /// To avoid infinite mutation loops, we need to apply a "do not mutate" annotation to all nodes in the subtree.
    /// </summary>
    private SyntaxNode ApplyDontMutateAnnotation(SyntaxNode node)
    {
        node = node.WithAdditionalAnnotations(Annotations.DontMutateAnnotation);

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

    private ExpressionSyntax WrapInDiscardMethod(ExpressionSyntax mutationSwitcher)
    {
        //Wrap the mutation switcher in an anonymous discard method to avoid build errors for certain mutation types.
        AssignmentExpressionSyntax discardAssignment =
            SyntaxFactory.AssignmentExpression(
                SyntaxKind.SimpleAssignmentExpression, 
                SyntaxFactory.IdentifierName("_"), 
                mutationSwitcher);

        return discardAssignment;
    }
}