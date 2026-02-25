using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;
using Mutator;
using Mutator.MutationImplementations;

namespace MutatorTests.MutationImplementations;

public class AddToSubtractMutatorTests
{
    private AddToSubtractMutator _mutator;

    [SetUp]
    public void SetUp()
    {
        _mutator = new AddToSubtractMutator();
    }

    [Test]
    public void AssertParamsCorrect()
    {
        Assert.That(_mutator.Category, Is.EqualTo(MutationCategory.Arithmetic));
        Assert.That(_mutator.Mutation, Is.EqualTo(SpecificMutation.AddToSubtract));
        Assert.That(_mutator.Kind, Is.EqualTo(SyntaxKind.AddExpression));
        Assert.That(_mutator.RequiredNodeType, Is.EqualTo(typeof(BinaryExpressionSyntax)));
    }

    [Test]
    public void GivenAddNode_WhenMutate_ThenGivesSubtractNodeWithSameLeftAndRight()
    {
        //Arrange
        SyntaxNode root = "'a' + 'b'".GetNodeOfType<BinaryExpressionSyntax>();


        //Act
        (SyntaxNode _, SyntaxAnnotation _, SyntaxNode mutatedNode) = _mutator.Mutate(root);

        //Assert
        Assert.That(mutatedNode.Kind, Is.EqualTo(SyntaxKind.SubtractExpression));
        SyntaxNode expected = "'a' - 'b'".GetNodeOfType<BinaryExpressionSyntax>();
        expected.AssertEquivalent(mutatedNode);
    }

    /// <summary>
    /// This test serves as the test for the <see cref="BaseMutationImplementation"/> class
    /// </summary>
    [Test]
    public void GivenValidMutation_ThenMutationSwitcherCreatedAsExpected()
    {
        //Arrange
        SyntaxNode root = "'a' + 'b'".GetNodeOfType<BinaryExpressionSyntax>();

        //Act
        (SyntaxNode mutationSwitcher, SyntaxAnnotation identififer, SyntaxNode _) = _mutator.Mutate(root);

        //Assert
        ParenthesizedExpressionSyntax expectedSwitcher = $"(Environment.GetEnvironmentVariable(variable: \"DarwingActiveMutationIndex\") == \"{identififer.Data}\" ? 'a' - 'b' : 'a' + 'b')".GetNodeOfType<ParenthesizedExpressionSyntax>();
        expectedSwitcher.AssertEquivalent(mutationSwitcher);
    }

    [Test]
    public void GivenWrongNodeType_WhenMutate_ThrowsMutationException()
    {
        Assert.Throws<MutationException>(() => _mutator.Mutate(SyntaxFactory.EmptyStatement()));
    }
}
