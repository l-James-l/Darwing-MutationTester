using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;
using Mutator;
using Mutator.MutationImplementations;

namespace MutatorTests.MutationImplementations;

public class IncrementToDecrementMutatorTests
{
    private IncrementToDecrementMutator _mutator;

    [SetUp]
    public void SetUp()
    {
        _mutator = new IncrementToDecrementMutator();
    }

    [Test]
    public void AssertParamsCorrect()
    {
        Assert.That(_mutator.Category, Is.EqualTo(MutationCategory.Arithmetic));
        Assert.That(_mutator.Mutation, Is.EqualTo(SpecificMutation.IncrementToDecrement));
        Assert.That(_mutator.Kind, Is.EqualTo(SyntaxKind.PostIncrementExpression));
        Assert.That(_mutator.RequiredNodeType, Is.EqualTo(typeof(PostfixUnaryExpressionSyntax)));
    }

    [Test]
    public void GivenPostIncrementNode_WhenMutate_ThenGivesPostDecrementNodeWithSameParam()
    {
        //Arrange
        SyntaxNode root = "5++".GetNodeOfType<PostfixUnaryExpressionSyntax>();

        //Act
        (SyntaxNode _, SyntaxAnnotation _, SyntaxNode mutatedNode) = _mutator.Mutate(root);

        //Assert
        Assert.That(mutatedNode.Kind, Is.EqualTo(SyntaxKind.PostDecrementExpression));
        SyntaxNode expected = "5--".GetNodeOfType<PostfixUnaryExpressionSyntax>();
        expected.AssertEquivalent(mutatedNode);
    }

    /// <summary>
    /// This test serves as the test for the <see cref="BaseMutationImplementation"/> class, 
    /// where mutations are wrapped in a discard assignment.
    /// </summary>
    [Test]
    public void GivenValidMutation_ThenMutationSwitcherCreatedAsExpected()
    {
        //Arrange
        SyntaxNode root = "5++".GetNodeOfType<PostfixUnaryExpressionSyntax>();

        //Act
        (SyntaxNode mutationSwitcher, SyntaxAnnotation identififer, SyntaxNode _) = _mutator.Mutate(root);

        //Assert
        AssignmentExpressionSyntax expectedSwitcher = 
            $"_ = (Environment.GetEnvironmentVariable(variable: \"DarwingActiveMutationIndex\") == \"{identififer.Data}\" ? 5-- : 5++)"
            .GetNodeOfType<AssignmentExpressionSyntax>();
        expectedSwitcher.AssertEquivalent(mutationSwitcher);
    }

    [Test]
    public void GivenWrongNodeType_WhenMutate_ThrowsMutationException()
    {
        Assert.Throws<MutationException>(() => _mutator.Mutate(SyntaxFactory.EmptyStatement()));
    }
}
