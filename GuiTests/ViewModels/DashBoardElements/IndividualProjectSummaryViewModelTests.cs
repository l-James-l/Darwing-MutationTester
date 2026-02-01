using GUI.ViewModels.DashBoardElements;
using Microsoft.CodeAnalysis;
using Models;
using NSubstitute;

namespace GuiTests.ViewModels.DashBoardElements;

public class IndividualProjectSummaryViewModelTests
{
    private IndividualProjectSummaryViewModel _vm;

    private IProjectContainer _project;

    [SetUp]
    public void SetUp()
    {
        _project = Substitute.For<IProjectContainer>();
        _project.ID.Returns(ProjectId.CreateNewId());
        _project.Name.Returns("TestProject");

        _vm = new(_project);
    }

    [Test]
    public void GivenInitialisation_ThenNameAndIDSet_AndCountsAt0()
    {
        //Arrange
        ProjectId projectId = ProjectId.CreateFromSerialized(Guid.NewGuid());
        _project.ID.Returns(projectId);
        
        //Act
        _vm = new(_project);

        //Asset
        Assert.That(_vm.ID, Is.EqualTo(projectId));
        Assert.That(_vm.Name, Is.EqualTo("TestProject"));
        Assert.That(_vm.KilledMutations, Is.EqualTo(0));
        Assert.That(_vm.SurvivedMutations, Is.EqualTo(0));
        Assert.That(_vm.TotalMutations, Is.EqualTo(0));
        Assert.That(_vm.MutationScore, Is.EqualTo(0));
    }

    [Test]
    public void GivenTotalOrKilledUpdated_ThenScoreUpdated()
    {
        //Act
        _vm.TotalMutations = 10;
        _vm.KilledMutations = 5;

        //Assert
        Assert.That(_vm.MutationScore, Is.EqualTo(50));
    }
}
