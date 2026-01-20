using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;

namespace GUI.ViewModels.ElementViewModels;

public class StatusBarViewModel : ViewModelBase
{
    private readonly IStatusTracker _statusTracker;

    public StatusBarViewModel(IStatusTracker statusTracker, IEventAggregator eventAggregator)
    {
        _statusTracker = statusTracker;

        eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Subscribe(OnOperationStatesChanged, ThreadOption.UIThread);
    }

    private void OnOperationStatesChanged()
    {
        LoadSolutionState = _statusTracker.CheckStatus(DarwingOperation.LoadSolution);
        BuildOperationState = _statusTracker.CheckStatus(DarwingOperation.BuildSolution);
        InitialTestRunState = _statusTracker.CheckStatus(DarwingOperation.TestUnmutatedSolution);
        MutantDiscoveryState = _statusTracker.CheckStatus(DarwingOperation.DiscoveringMutants);
        BuildingMutatedSolutionState = _statusTracker.CheckStatus(DarwingOperation.BuildingMutatedSolution);
        TestingMutantsState = _statusTracker.CheckStatus(DarwingOperation.TestMutants);
        UpdateCompletionPercentage();
    }

    public float ProgressBarPercentage
    {
        get => _progressBarPercentage;
        set
        {
            _progressBarPercentage = value;
            OnPropertyChanged();
        }
    }
    private float _progressBarPercentage;

    private void UpdateCompletionPercentage()
    {
        const float eachOperationPercentage = 0.2f; // Since there are 5 operations contributing to the progress bar
        float percentage = 0.0f;
        if (LoadSolutionState is OperationStates.Succeeded)
        {
            percentage += eachOperationPercentage;
        }
        if (BuildOperationState is OperationStates.Succeeded)
        {
            // We give half weight to the build operation to make it clear that continuing is a manual operation
            percentage += eachOperationPercentage/2;
        }
        if (InitialTestRunState is not OperationStates.NotStarted)
        {
            // User has continued so now complete the building section.
            percentage += eachOperationPercentage / 2;
        }
        if (InitialTestRunState is OperationStates.Succeeded)
        {
            percentage += eachOperationPercentage;
        }
        if (MutantDiscoveryState is OperationStates.Succeeded)
        {
            percentage += eachOperationPercentage;
        }
        if (BuildingMutatedSolutionState is OperationStates.Succeeded)
        {
            percentage += eachOperationPercentage;
        }

        // We ignore the last operation (TestingMutantsState) for the progress bar percentage
        // because the bar doesn't extend past that operation, so once were at that point, the bar is full.
        ProgressBarPercentage = percentage;
    }

    public OperationStates LoadSolutionState
    {
        get => _loadSolutionState;
        set
        {
            _loadSolutionState = value;
            OnPropertyChanged();
        }
    }
    private OperationStates _loadSolutionState;

    public OperationStates BuildOperationState 
    {
        get => _buildOperationState; 
        set
        {
            _buildOperationState = value;
            OnPropertyChanged();
        } 
    }
    private OperationStates _buildOperationState;

    public OperationStates InitialTestRunState 
    {
        get => _initialTestRunState;
        set
        {
            _initialTestRunState = value;
            OnPropertyChanged();
        } 
    }
    private OperationStates _initialTestRunState;

    public OperationStates MutantDiscoveryState
    {
        get => _mutantDiscoveryState;
        set
        {
            _mutantDiscoveryState = value;
            OnPropertyChanged();
        }
    }
    private OperationStates _mutantDiscoveryState;


    public OperationStates BuildingMutatedSolutionState
    {
        get => _buildingMutatedSolutionState;
        set
        {
            _buildingMutatedSolutionState = value;
            OnPropertyChanged();
        }
    }
    private OperationStates _buildingMutatedSolutionState;


    public OperationStates TestingMutantsState
    {
        get => _testingMutantsState;
        set
        {
            _testingMutantsState = value;
            OnPropertyChanged();
        }
    }
    private OperationStates _testingMutantsState;
}
