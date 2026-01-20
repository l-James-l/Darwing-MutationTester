using Core;
using Models;
using NSubstitute;

namespace CoreTests;

public class SolutionProviderTests
{
    private SolutionProvider _solutionProvider;

    [SetUp]
    public void Setup()
    {
        _solutionProvider = new SolutionProvider();
    }

    [Test]
    public void ThrowsInvalidOperationException_WhenNoSolutionLoaded()
    {
        // Arrange & Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() => { var _ = _solutionProvider.SolutionContainer; });
        Assert.That(ex.Message, Is.EqualTo("Attempted to retrieve a solution before one has been loaded."));
    }

    [Test]
    public void IsAvailable_ReturnsFalse_WhenNoSolutionLoaded()
    {
        // Arrange & Act
        var isAvailable = _solutionProvider.IsAvailable;
     
        // Assert
        Assert.IsFalse(isAvailable);
    }

    [Test]
    public void IsAvailable_ReturnsTrue_AfterSolutionLoaded()
    {
        // Arrange
        ISolutionContainer mockSolution = Substitute.For<ISolutionContainer>();
        _solutionProvider.NewSolution(mockSolution);
     
        // Act
        var isAvailable = _solutionProvider.IsAvailable;
        
        // Assert
        Assert.IsTrue(isAvailable);
        Assert.That(_solutionProvider.SolutionContainer, Is.SameAs(mockSolution));
    }
}
