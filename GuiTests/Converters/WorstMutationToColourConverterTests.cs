using GUI.Converters;
using GUI.ViewModels.SolutionExplorerElements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models;
using Models.Enums;
using NSubstitute;
using System.Windows.Media;

namespace GuiTests.Converters;

public class WorstMutationToColourConverterTests
{
    private MutationViewModel CreateMutation(MutantStatus status)
    {
        DiscoveredMutation newMutation = new(new SyntaxAnnotation(), SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), Substitute.For<IEventAggregator>(), 0, 0)
        {
            Status = status
        };
        return new MutationViewModel(newMutation);
    }

    [Test]
    public void GivenConverter_WhenConverting_ThenReturnsExpectedColour()
    {
        // Arrange
        var converter = new WorstMutationToColourConverter();

        // Act & Assert
        Assert.That(converter.Convert(new List<MutationViewModel> { CreateMutation(MutantStatus.Killed), CreateMutation(MutantStatus.Killed) }, null, null, null), Is.EqualTo(Brushes.Green));
        Assert.That(converter.Convert(new List<MutationViewModel> { CreateMutation(MutantStatus.Killed), CreateMutation(MutantStatus.Available) }, null, null, null), Is.EqualTo(Brushes.Orange));
        Assert.That(converter.Convert(new List<MutationViewModel> { CreateMutation(MutantStatus.Killed), CreateMutation(MutantStatus.Survived) }, null, null, null), Is.EqualTo(Brushes.Red));
    }
    
    [Test]
    public void GivenEmptyCollection_WhenConverting_ThenReturnsTransparentBrush()
    {
        // Arrange
        var converter = new WorstMutationToColourConverter();
        var emptyCollection = new List<MutationViewModel>();
        
        // Act
        var result = converter.Convert(emptyCollection, null, null, null);
     
        // Assert
        Assert.That(result, Is.EqualTo(Brushes.Transparent));
    }

}
