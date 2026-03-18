using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;
using Mutator;
using Mutator.MutationImplementations.Relational;

namespace MutatorTests.MutationImplementations.Relational;

public class LessThanToGreaterThanOrEqualToMutatorTests
{
    private LessThanToGreaterThanOrEqualToMutator _mutator;

    [SetUp]
    public void SetUp()
    {
        _mutator = new LessThanToGreaterThanOrEqualToMutator();
    }

    [Test]
    public void AssertParamsCorrect()
    {
        Assert.That(_mutator.Category, Is.EqualTo(MutationCategory.Relational));
        Assert.That(_mutator.Mutation, Is.EqualTo(SpecificMutation.LessThanToGreaterThanOrEqualTo));
        Assert.That(_mutator.Kind, Is.EqualTo(SyntaxKind.LessThanExpression));
        Assert.That(_mutator.RequiredNodeType, Is.EqualTo(typeof(BinaryExpressionSyntax)));
    }

    [Test]
    public void GivenLessThanNode_WhenMutate_ThenGivesGreaterThanOrEqualNodeWithSameLeftAndRight()
    {
        //Arrange
        SyntaxNode root = "'a' < 'b'".GetNodeOfType<BinaryExpressionSyntax>();

        //Act
        (SyntaxNode _, SyntaxAnnotation _, SyntaxNode mutatedNode) = _mutator.Mutate(root);

        //Assert
        Assert.That(mutatedNode.Kind, Is.EqualTo(SyntaxKind.GreaterThanOrEqualExpression));
        SyntaxNode expected = "'a' >= 'b'".GetNodeOfType<BinaryExpressionSyntax>();
        expected.AssertEquivalent(mutatedNode);
    }

    [Test]
    public void GivenWrongNodeType_WhenMutate_ThrowsMutationException()
    {
        Assert.Throws<MutationException>(() => _mutator.Mutate(SyntaxFactory.EmptyStatement()));
    }
}
