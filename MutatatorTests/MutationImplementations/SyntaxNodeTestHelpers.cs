using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Mutator;

namespace MutatorTests.MutationImplementations;

/// <summary>
/// Class of static methods to help in the testing of syntax node behaviour
/// </summary>
internal static class SyntaxNodeTestHelpers
{
    /// <summary>
    /// From a string of c# code, find the first node of type T
    /// </summary>
    public static T GetNodeOfType<T>(this string text) where T : SyntaxNode
    {
        SyntaxNode node = CSharpSyntaxTree.ParseText(text).GetRoot();
        return GetNodeOfType<T>(node) ?? throw new MutationException($"Unable to find a child node of type {typeof(T).Name}");

    }

    /// <summary>
    /// From a syntax node, find the first child or self of type T
    /// </summary>
    public static T? GetNodeOfType<T>(this SyntaxNode node) where T : SyntaxNode
    {
        if (node is T t)
        {
            return t.NormalizeWhitespace();
        }
        IEnumerable<SyntaxNode> children = node.ChildNodes();
        foreach (SyntaxNode child in children)
        {
            if (child.GetNodeOfType<T>() is T t1)
            {
                return t1.NormalizeWhitespace();
            }
        }
        return null;
    }

    /// <summary>
    /// Are 2 syntax nodes logically equivalent, including trivia and children
    /// </summary>
    /// <param name="node1"></param>
    /// <param name="node2"></param>
    public static void AssertEquivalent(this SyntaxNode node1, SyntaxNode node2)
    {
        if (node1.GetType() != node2.GetType())
        {
            Assert.Fail($"nodes are of different types. Node1: {node1.GetType()}. Node2: {node2.GetType()}");
        }
        if (!node1.IsKind(node2.Kind()))
        {
            Assert.Fail($"Nodes have different syntax kinds. Node1: {node1.Kind()}. Node2: {node2.Kind()}.");
        }

        AssertSameTrivia(node1.GetLeadingTrivia(), node2.GetLeadingTrivia());
        AssertSameTrivia(node1.GetTrailingTrivia(), node2.GetTrailingTrivia());

        List<SyntaxNode> node1Children = node1.ChildNodes().ToList();
        List<SyntaxNode> node2Children = node2.ChildNodes().ToList();
        if (node1Children.Count() != node2Children.Count())
        {
            Assert.Fail($"nodes have a different number of children. Node1: {node1Children.Count}, Node2: {node2Children.Count}.");
        }

        for (int i = 0; i < node1Children.Count(); i++)
        {
            node1Children[i].AssertEquivalent(node2Children[i]);
        }
    }

    private static void AssertSameTrivia(SyntaxTriviaList node1Trivia, SyntaxTriviaList node2Trivia)
    {
        if (node1Trivia.Count != node2Trivia.Count)
        {
            Assert.Fail($"Different trivia counts. node1: {node1Trivia}. node2: {node2Trivia}.");
        }

        for (int t = 0; t < node1Trivia.Count; t++)
        {
            SyntaxTrivia t1Trivia = node1Trivia[t];
            SyntaxTrivia t2Trivia = node2Trivia[t];
            if (!t1Trivia.Span.Equals(t2Trivia.Span))
            {
                Assert.Fail($"Spans of trivia are not equal. Node1Trivia: {t1Trivia.Span}. Node2Trivia: {t2Trivia.Span}.");
            }
            if (!t1Trivia.IsKind(t2Trivia.Kind()))
            {
                Assert.Fail($"Kinds of trivia are not equal. Node1Trivia: {t1Trivia.Kind()}. Node2Trivia: {t2Trivia.Kind()}.");
            }
        }
    }
}
