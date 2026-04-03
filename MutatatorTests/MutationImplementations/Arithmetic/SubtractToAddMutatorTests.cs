using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;
using Mutator;
using Mutator.MutationImplementations.Arithmetic;

namespace MutatorTests.MutationImplementations.Arithmetic;

public class SubtractToAddMutatorTests
{
    private SubtractToAddMutator _mutator;

    [SetUp]
    public void SetUp()
    {
        _mutator = new SubtractToAddMutator();
    }

    [Test]
    public void AssertParamsCorrect()
    {
        Assert.That(_mutator.Category, Is.EqualTo(MutationCategory.Arithmetic));
        Assert.That(_mutator.Mutation, Is.EqualTo(SpecificMutation.SubtractToAdd));
        Assert.That(_mutator.Kind, Is.EqualTo(SyntaxKind.SubtractExpression));
        Assert.That(_mutator.RequiredNodeType, Is.EqualTo(typeof(BinaryExpressionSyntax)));
    }

    [Test]
    public void GivenSubtractNode_WhenMutate_ThenGivesAddNodeWithSameLeftAndRight()
    {
        //Arrange
        SyntaxNode root = "'a' - 'b'".GetNodeOfType<BinaryExpressionSyntax>();

        //Act
        (SyntaxNode _, SyntaxAnnotation _, SyntaxNode mutatedNode) = _mutator.Mutate(root);

        //Assert
        Assert.That(mutatedNode.Kind, Is.EqualTo(SyntaxKind.AddExpression));
        SyntaxNode expected = "'a' + 'b'".GetNodeOfType<BinaryExpressionSyntax>();
        expected.AssertEquivalent(mutatedNode);
    }

    [Test]
    public void GivenWrongNodeType_WhenMutate_ThrowsMutationException()
    {
        Assert.Throws<MutationException>(() => _mutator.Mutate(SyntaxFactory.EmptyStatement()));
    }
}
