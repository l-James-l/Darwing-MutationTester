using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Models.Enums;
using Mutator;
using Mutator.MutationImplementations;

namespace MutatorTests;

public class MutationImplementationProviderTests
{
    private MutationImplementationProvider _mutationImplementationProvider;

    [SetUp]
    public void SetUp()
    {
        IEnumerable<IMutationImplementation> mutators = [new TestMutator1(), new TestMutator2()];

        _mutationImplementationProvider = new MutationImplementationProvider(mutators);
    }

    [Test]
    public void GivenNoSupportedMutator_WhenCanMutate_ThenReturnsFalse_AndMutatorNull()
    {
        //Arrange
        string codeSnippit = "public void Method1(int a) {}";
        SyntaxNode root = SyntaxFactory.ParseSyntaxTree(codeSnippit).GetRoot();

        //Act
        bool result = _mutationImplementationProvider.CanMutate(root, out IMutationImplementation? mutator);

        //Assert
        Assert.That(result, Is.False);
        Assert.That(mutator, Is.Null);
    }


    [Test]
    public void GivenSupportedMutator_WhenCanMutate_ThenReturnsTrue_AndMutatorNotNull()
    {
        //Arrange
        //string codeSnippit = "public void Method1(int a) {}";
        //SyntaxNode root = SyntaxFactory.ParseSyntaxTree(codeSnippit).GetRoot();
        SyntaxNode node = SyntaxFactory.BinaryExpression(SyntaxKind.AddExpression, 
            SyntaxFactory.BaseExpression(), 
            SyntaxFactory.BaseExpression());

        //Act
        bool result = _mutationImplementationProvider.CanMutate(node, out IMutationImplementation? mutator);

        //Assert
        Assert.That(result, Is.True);
        Assert.That(mutator, Is.InstanceOf<TestMutator2>());
    }
}

file class TestMutator1 : IMutationImplementation
{
    public SpecifcMutation Mutation => throw new NotImplementedException();

    public MutationCategory Category => throw new NotImplementedException();

    public SyntaxKind Kind => SyntaxKind.PostIncrementExpression;

    public Type RequiredNodeType => typeof(PostfixUnaryExpressionSyntax);

    (SyntaxNode mutatedNode, SyntaxAnnotation identififer) IMutationImplementation.Mutate(SyntaxNode node)
    {
        throw new NotImplementedException();
    }
}

file class TestMutator2 : IMutationImplementation
{
    public SpecifcMutation Mutation => throw new NotImplementedException();

    public MutationCategory Category => throw new NotImplementedException();

    public SyntaxKind Kind => SyntaxKind.AddExpression;

    public Type RequiredNodeType => typeof(BinaryExpressionSyntax);

    (SyntaxNode mutatedNode, SyntaxAnnotation identififer) IMutationImplementation.Mutate(SyntaxNode node)
    {
        throw new NotImplementedException();
    }
}


