using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;
using Mutator;
using Mutator.MutationImplementations;

namespace MutatorTests.MutationImplementations;

public class NotEqualToEqualMutatorTests
{
    private NotEqualToEqualMutator _mutator;

    [SetUp]
    public void SetUp()
    {
        _mutator = new NotEqualToEqualMutator();
    }


    [Test]
    public void AssertParamsCorrect()
    {
        Assert.That(_mutator.Category, Is.EqualTo(MutationCategory.Logical));
        Assert.That(_mutator.Mutation, Is.EqualTo(SpecificMutation.NotEqualToEqual));
        Assert.That(_mutator.Kind, Is.EqualTo(SyntaxKind.NotEqualsExpression));
        Assert.That(_mutator.RequiredNodeType, Is.EqualTo(typeof(BinaryExpressionSyntax)));
    }

    [Test]
    public void GivenNotEqualNode_WhenMutate_ThenGivesEqualsNodeWithSameLeftAndRight()
    {
        //Arrange
        SyntaxNode root = "'a' != 'b'".GetNodeOfType<BinaryExpressionSyntax>();

        //Act
        (SyntaxNode _, SyntaxAnnotation _, SyntaxNode mutatedNode) = _mutator.Mutate(root);

        //Assert
        Assert.That(mutatedNode.Kind, Is.EqualTo(SyntaxKind.EqualsExpression));
        SyntaxNode expected = "'a' == 'b'".GetNodeOfType<BinaryExpressionSyntax>();
        expected.AssertEquivalent(mutatedNode);
    }

    [Test]
    public void GivenWrongNodeType_WhenMutate_ThrowsMutationException()
    {
        Assert.Throws<MutationException>(() => _mutator.Mutate(SyntaxFactory.EmptyStatement()));
    }
}
