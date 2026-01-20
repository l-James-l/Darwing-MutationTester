using GUI.ViewModels.ElementViewModels;
using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;
using NSubstitute;
using System.Windows.Threading;

namespace GuiTests.ViewModels.ElementViewModels;

public class StatusBarViewModelTests
{
    private StatusBarViewModel _statusBarViewModel;

    private IEventAggregator _eventAggregator;
    private IStatusTracker _statusTracker;

    private DarwingOperationStatesChangedEvent _statesChangedEvent;

    [SetUp]
    public void SetUp()
    {
        Dispatcher.CurrentDispatcher.Invoke(() => { _eventAggregator = Substitute.For<IEventAggregator>(); });
        _statusTracker = Substitute.For<IStatusTracker>();

        _statesChangedEvent = new();
        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Returns(_statesChangedEvent);

        Dispatcher.CurrentDispatcher.Invoke(() => { _statusBarViewModel = new StatusBarViewModel(_statusTracker, _eventAggregator); } );
    }

    [Test, /*Apartment(ApartmentState.STA),*/ Explicit("TODO: Subscription fails due to UI thread use.")]
    public void GivenNoStatesStarted_WhenReceivedUpdate_StatesReflected_AndProgressBarEmpty()
    {
        _statusTracker.CheckStatus(DarwingOperation.LoadSolution).Returns(OperationStates.NotStarted);
        _statusTracker.CheckStatus(DarwingOperation.BuildSolution).Returns(OperationStates.NotStarted);
        _statusTracker.CheckStatus(DarwingOperation.TestUnmutatedSolution).Returns(OperationStates.NotStarted);
        _statusTracker.CheckStatus(DarwingOperation.DiscoveringMutants).Returns(OperationStates.NotStarted);
        _statusTracker.CheckStatus(DarwingOperation.BuildingMutatedSolution).Returns(OperationStates.NotStarted);
        _statusTracker.CheckStatus(DarwingOperation.TestMutants).Returns(OperationStates.NotStarted);

        //Act
        Dispatcher.CurrentDispatcher.InvokeAsync(_statesChangedEvent.Publish).Wait();

        //Assert
        Assert.That(_statusBarViewModel.LoadSolutionState is OperationStates.NotStarted);
        Assert.That(_statusBarViewModel.BuildOperationState is OperationStates.NotStarted);
        Assert.That(_statusBarViewModel.InitialTestRunState is OperationStates.NotStarted);
        Assert.That(_statusBarViewModel.MutantDiscoveryState is OperationStates.NotStarted);
        Assert.That(_statusBarViewModel.BuildingMutatedSolutionState is OperationStates.NotStarted);
        Assert.That(_statusBarViewModel.TestingMutantsState is OperationStates.NotStarted);
        Assert.That(_statusBarViewModel.ProgressBarPercentage is 0f);
    }
}
