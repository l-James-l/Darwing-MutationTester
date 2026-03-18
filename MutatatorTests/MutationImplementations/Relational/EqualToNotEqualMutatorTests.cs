using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;
using Mutator;
using Mutator.MutationImplementations.Relational;

namespace MutatorTests.MutationImplementations.Relational;

public class EqualToNotEqualMutatorTests
{
    private EqualToNotEqualMutator _mutator;

    [SetUp]
    public void SetUp()
    {
        _mutator = new EqualToNotEqualMutator();
    }


    [Test]
    public void AssertParamsCorrect()
    {
        Assert.That(_mutator.Category, Is.EqualTo(MutationCategory.Logical));
        Assert.That(_mutator.Mutation, Is.EqualTo(SpecificMutation.EqualToNotEqual));
        Assert.That(_mutator.Kind, Is.EqualTo(SyntaxKind.EqualsExpression));
        Assert.That(_mutator.RequiredNodeType, Is.EqualTo(typeof(BinaryExpressionSyntax)));
    }

    [Test]
    public void GivenEqualsNode_WhenMutate_ThenGivesNotEqualsNodeWithSameLeftAndRight()
    {
        //Arrange
        SyntaxNode root = "'a' == 'b'".GetNodeOfType<BinaryExpressionSyntax>();

        //Act
        (SyntaxNode _, SyntaxAnnotation _, SyntaxNode mutatedNode) = _mutator.Mutate(root);

        //Assert
        Assert.That(mutatedNode.Kind, Is.EqualTo(SyntaxKind.NotEqualsExpression));
        SyntaxNode expected = "'a' != 'b'".GetNodeOfType<BinaryExpressionSyntax>();
        expected.AssertEquivalent(mutatedNode);
    }

    [Test]
    public void GivenWrongNodeType_WhenMutate_ThrowsMutationException()
    {
        Assert.Throws<MutationException>(() => _mutator.Mutate(SyntaxFactory.EmptyStatement()));
    }
}
