using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;

namespace GUI.ViewModels.DashBoardElements;

public class StatusBarViewModel : ViewModelBase
{
    private readonly IStatusTracker _statusTracker;

    public StatusBarViewModel(IStatusTracker statusTracker, IEventAggregator eventAggregator)
    {
        _statusTracker = statusTracker;

        eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Subscribe(_ => OnOperationStatesChanged(), ThreadOption.UIThread);
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
        get; 
        set => SetProperty(ref field, value);
    }

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
        get; 
        set => SetProperty(ref field, value);
    }

    public OperationStates BuildOperationState
    {
        get; 
        set => SetProperty(ref field, value);
    }

    public OperationStates InitialTestRunState
    {
        get; 
        set => SetProperty(ref field, value);
    }

    public OperationStates MutantDiscoveryState
    {
        get; 
        set => SetProperty(ref field, value);
    }


    public OperationStates BuildingMutatedSolutionState
    {
        get; 
        set => SetProperty(ref field, value);
    }


    public OperationStates TestingMutantsState
    {
        get; 
        set => SetProperty(ref field, value);
    }
}
