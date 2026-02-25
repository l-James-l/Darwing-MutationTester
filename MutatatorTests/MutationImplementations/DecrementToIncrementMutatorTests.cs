using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;
using Mutator;
using Mutator.MutationImplementations;

namespace MutatorTests.MutationImplementations;

public class DecrementToIncrementMutatorTests
{
    private DecrementToIncrementMutator _mutator;

    [SetUp]
    public void SetUp()
    {
        _mutator = new DecrementToIncrementMutator();
    }

    [Test]
    public void AssertParamsCorrect()
    {
        Assert.That(_mutator.Category, Is.EqualTo(MutationCategory.Arithmetic));
        Assert.That(_mutator.Mutation, Is.EqualTo(SpecificMutation.DecrementToIncrement));
        Assert.That(_mutator.Kind, Is.EqualTo(SyntaxKind.PostDecrementExpression));
        Assert.That(_mutator.RequiredNodeType, Is.EqualTo(typeof(PostfixUnaryExpressionSyntax)));
    }

    [Test]
    public void GivenPostDecrementNode_WhenMutate_ThenGivesPostIncrementWithSameParam()
    {
        //Arrange
        SyntaxNode root = "5--".GetNodeOfType<PostfixUnaryExpressionSyntax>();

        //Act
        (SyntaxNode _, SyntaxAnnotation _, SyntaxNode mutatedNode) = _mutator.Mutate(root);

        //Assert
        Assert.That(mutatedNode.Kind, Is.EqualTo(SyntaxKind.PostIncrementExpression));
        SyntaxNode expected = "5++".GetNodeOfType<PostfixUnaryExpressionSyntax>();
        expected.AssertEquivalent(mutatedNode);
    }

    [Test]
    public void GivenWrongNodeType_WhenMutate_ThrowsMutationException()
    {
        Assert.Throws<MutationException>(() => _mutator.Mutate(SyntaxFactory.EmptyStatement()));
    }
}
