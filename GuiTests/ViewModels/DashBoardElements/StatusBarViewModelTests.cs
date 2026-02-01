using GUI.ViewModels.DashBoardElements;
using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;
using NSubstitute;

namespace GuiTests.ViewModels.DashBoardElements;

public class StatusBarViewModelTests
{
    private IStatusTracker _statusTracker;
    private IEventAggregator _eventAggregator;
    private DarwingOperationStatesChangedEvent _statesChangedEvent;

    private Action<DarwingOperation> _handler = default!; //Assign default to make compiler happy 

    private StatusBarViewModel _vm;

    [SetUp]
    public void SetUp()
    {
        _statusTracker = Substitute.For<IStatusTracker>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _statesChangedEvent = Substitute.For<DarwingOperationStatesChangedEvent>();

        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Returns(_statesChangedEvent);

        _statesChangedEvent
            .When(x => x.Subscribe(Arg.Any<Action<DarwingOperation>>(), Arg.Any<ThreadOption>()))
            .Do(ci => _handler = ci.Arg<Action<DarwingOperation>>());

        _vm = new StatusBarViewModel(_statusTracker, _eventAggregator);
    }

    [Test]
    public void GivenConstructed_ThenSubscriptionMade()
    {
        Assert.That(_handler, Is.Not.Null);
    }

    [Test]
    public void GivenStatusTrackerValues_WhenOperationStateChanges_ThenAllStatesAreUpdated()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution)
            .Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.BuildSolution)
            .Returns(OperationStates.Ongoing);

        _statusTracker.CheckStatus(DarwingOperation.TestUnmutatedSolution)
            .Returns(OperationStates.NotStarted);

        _statusTracker.CheckStatus(DarwingOperation.DiscoveringMutants)
            .Returns(OperationStates.Failed);

        _statusTracker.CheckStatus(DarwingOperation.BuildingMutatedSolution)
            .Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.TestMutants)
            .Returns(OperationStates.Ongoing);

        // Act
        _handler.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.That(_vm.LoadSolutionState, Is.EqualTo(OperationStates.Succeeded));
        Assert.That(_vm.BuildOperationState, Is.EqualTo(OperationStates.Ongoing));
        Assert.That(_vm.InitialTestRunState, Is.EqualTo(OperationStates.NotStarted));
        Assert.That(_vm.MutantDiscoveryState, Is.EqualTo(OperationStates.Failed));
        Assert.That(_vm.BuildingMutatedSolutionState, Is.EqualTo(OperationStates.Succeeded));
        Assert.That(_vm.TestingMutantsState, Is.EqualTo(OperationStates.Ongoing));
    }

    [Test]
    public void GivenNoOperationsSucceeded_WhenOperationStateChanges_ThenProgressIsZero()
    {
        // Arrange
        _statusTracker.CheckStatus(Arg.Any<DarwingOperation>()).Returns(OperationStates.NotStarted);

        // Act
        _handler.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.That(_vm.ProgressBarPercentage, Is.Zero);
    }

    [Test]
    public void GivenLoadSolutionSucceeded_WhenOperationStateChanges_ThenProgressIsTwentyPercent()
    {
        // Arrange
        _statusTracker.CheckStatus(Arg.Any<DarwingOperation>()).Returns(OperationStates.NotStarted);
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);

        // Act
        _handler.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.That(_vm.ProgressBarPercentage, Is.EqualTo(0.2f));
    }

    [Test]
    public void GivenBuildSucceededButInitialTestsNotStarted_WhenOperationStateChanges_ThenProgressIsThirtyPercent()
    {
        // Load succeeded → 0.2  
        // Build succeeded half → +0.1  
        // Initial not started → no extra

        // Arrange
        _statusTracker.CheckStatus(Arg.Any<DarwingOperation>()).Returns(OperationStates.NotStarted);

        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.TestUnmutatedSolution).Returns(OperationStates.NotStarted);


        // Act
        _handler.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.That(_vm.ProgressBarPercentage, Is.EqualTo(0.3f));
    }

    [Test]
    public void GivenInitialTestsStartedButNotCompleted_WhenOperationStateChanges_ThenBuildSectionIsCompleted()
    {
        // Load 0.2  
        // Build half 0.1  
        // Initial not NotStarted → other half 0.1  

        // Arrange
        _statusTracker.CheckStatus(Arg.Any<DarwingOperation>()).Returns(OperationStates.NotStarted);

        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.TestUnmutatedSolution).Returns(OperationStates.Ongoing);

        // Act
        _handler.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.That(_vm.ProgressBarPercentage, Is.EqualTo(0.4f));
    }

    [Test]
    public void GivenInitialTestsSucceeded_WhenOperationStateChanges_ThenProgressIncludesInitialTestSection()
    {
        // Load 0.2  
        // Build full 0.2  
        // Initial succeeded 0.2  

        // Arrange
        _statusTracker.CheckStatus(Arg.Any<DarwingOperation>()).Returns(OperationStates.NotStarted);

        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.TestUnmutatedSolution).Returns(OperationStates.Succeeded);

        // Act
        _handler.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.That(_vm.ProgressBarPercentage, Is.EqualTo(0.6f));
    }

    [Test]
    public void GivenAllOperationsExceptTestingMutantsSucceeded_WhenOperationStateChanges_ThenProgressIsFull()
    {
        // Arrange
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.TestUnmutatedSolution).Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.DiscoveringMutants).Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.BuildingMutatedSolution).Returns(OperationStates.Succeeded);

        _statusTracker.CheckStatus(DarwingOperation.TestMutants).Returns(OperationStates.Ongoing);

        // Act
        _handler.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.That(_vm.ProgressBarPercentage, Is.EqualTo(1.0f));
    }

    [Test]
    public void GivenTestingMutantsSucceeded_WhenOperationStateChanges_ThenProgressDoesNotIncreaseBeyondFull()
    {
        // Arrange
        _statusTracker.CheckStatus(Arg.Any<DarwingOperation>()).Returns(OperationStates.Succeeded);

        // Act
        _handler.Invoke(DarwingOperation.TestMutants);

        // Assert
        Assert.That(_vm.ProgressBarPercentage, Is.EqualTo(1.0f));
    }
}