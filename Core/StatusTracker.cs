using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;
using Serilog;

namespace Core;

/// <summary>
/// Will track the overall status of the mutation testing process.
/// This class will subscribe to various events and update the status accordingly.
/// Used by the UI to display current status, and by other classes to check if certain actions can be performed.
/// </summary>
public class StatusTracker : IStatusTracker
{
    private Dictionary<DarwingOperation, OperationStates> _operations = new()
    {
        { DarwingOperation.Idle, OperationStates.Ongoing },
        { DarwingOperation.LoadSolution, OperationStates.NotStarted },
        { DarwingOperation.BuildSolution, OperationStates.NotStarted },
        { DarwingOperation.TestUnmutatedSolution, OperationStates.NotStarted  },
        { DarwingOperation.DiscoveringMutants, OperationStates.NotStarted  },
        { DarwingOperation.BuildingMutatedSolution, OperationStates.NotStarted  },
        { DarwingOperation.TestMutants, OperationStates.NotStarted  }
    };
    private readonly IEventAggregator _eventAggregator;

    private DarwingOperation _currentOperation => _operations.Single(kvp => kvp.Value == OperationStates.Ongoing).Key;

    public StatusTracker(IEventAggregator eventAggregator)
    {
        _eventAggregator = eventAggregator;
    }

    public OperationStates CheckStatus(DarwingOperation operation)
    {
        _operations.TryGetValue(operation, out OperationStates state);
        return state;
    }

    public bool TryStartOperation(DarwingOperation operation)
    {
        if (ValidOperation(operation))
        {
            Log.Information("Starting operation: {operation}", operation);
            _operations[DarwingOperation.Idle] = OperationStates.NotStarted;
            _operations[operation] = OperationStates.Ongoing;
            ResetStatesIfRegressing(operation);
            _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Publish(operation);
            return true;
        }

        Log.Warning("Attempted to start invalid operation stage: {operation}", operation);
        return false;        
    }

    private void ResetStatesIfRegressing(DarwingOperation operation)
    {
        // If we are starting an operation that is earlier in the sequence than the current one,
        // we need to reset the states of the subsequent operations.
        // Realistically, this should only happen with LoadSolution, BuildSolution & TestUnmutatedSolution
        switch (operation)
        {
            case DarwingOperation.LoadSolution:
                _operations[DarwingOperation.BuildSolution] = OperationStates.NotStarted;
                _operations[DarwingOperation.TestUnmutatedSolution] = OperationStates.NotStarted;
                _operations[DarwingOperation.DiscoveringMutants] = OperationStates.NotStarted;
                _operations[DarwingOperation.BuildingMutatedSolution] = OperationStates.NotStarted;
                _operations[DarwingOperation.TestMutants] = OperationStates.NotStarted;
                break;
            case DarwingOperation.BuildSolution:
                _operations[DarwingOperation.TestUnmutatedSolution] = OperationStates.NotStarted;
                _operations[DarwingOperation.DiscoveringMutants] = OperationStates.NotStarted;
                _operations[DarwingOperation.BuildingMutatedSolution] = OperationStates.NotStarted;
                _operations[DarwingOperation.TestMutants] = OperationStates.NotStarted;
                break;
            case DarwingOperation.TestUnmutatedSolution:
                _operations[DarwingOperation.DiscoveringMutants] = OperationStates.NotStarted;
                _operations[DarwingOperation.BuildingMutatedSolution] = OperationStates.NotStarted;
                _operations[DarwingOperation.TestMutants] = OperationStates.NotStarted;
                break;
            case DarwingOperation.DiscoveringMutants:
                _operations[DarwingOperation.BuildingMutatedSolution] = OperationStates.NotStarted;
                _operations[DarwingOperation.TestMutants] = OperationStates.NotStarted;
                break;
            case DarwingOperation.TestMutants:
                // No further operations to reset
                break;
        }
    }

    public void FinishOperation(DarwingOperation operation, bool success)
    {
        if (_currentOperation != operation)
        {
            throw new InvalidOperationException($"Cannot finish operation {operation} because current operation is {_currentOperation}");
        }

        if (success)
        {
            _operations[operation] = OperationStates.Succeeded;
        }
        else
        {
            _operations[operation] = OperationStates.Failed;
        }
        _operations[DarwingOperation.Idle] = OperationStates.Ongoing;
        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Publish(operation);
    }

    private bool ValidOperation(DarwingOperation operation)
    {
        switch (operation)
        {
            case DarwingOperation.LoadSolution:
                return CanLoadSolution();
            case DarwingOperation.BuildSolution:
                return CanBuildSolution();
            case DarwingOperation.TestUnmutatedSolution:
                return CanTestUnmutatedSolution();
            case DarwingOperation.DiscoveringMutants:
                return CanStartDiscoveringMutants();
            case DarwingOperation.BuildingMutatedSolution:
                return CanBuildMutatedSolution();
            case DarwingOperation.TestMutants:
                return CanTestMutants();
            default:
                throw new ArgumentOutOfRangeException($"Unknown operation requested: {operation}");
        }
    }


    private bool CanBuildSolution()
    {
        return _currentOperation is DarwingOperation.Idle 
            && _operations[DarwingOperation.LoadSolution] is OperationStates.Succeeded;
    }

    private bool CanLoadSolution()
    {
        return _currentOperation is DarwingOperation.Idle;
    }

    private bool CanTestUnmutatedSolution()
    {
        return _currentOperation is DarwingOperation.Idle 
            && _operations[DarwingOperation.LoadSolution] is OperationStates.Succeeded
            && _operations[DarwingOperation.BuildSolution] is OperationStates.Succeeded;
    }

    public bool CanStartDiscoveringMutants()
    {
        return _currentOperation is DarwingOperation.Idle
            && _operations[DarwingOperation.LoadSolution] is OperationStates.Succeeded
            && _operations[DarwingOperation.BuildSolution] is OperationStates.Succeeded
            && _operations[DarwingOperation.TestUnmutatedSolution] is OperationStates.Succeeded;

    }

    private bool CanBuildMutatedSolution()
    {
        return _currentOperation is DarwingOperation.Idle
            && _operations[DarwingOperation.LoadSolution] is OperationStates.Succeeded
            && _operations[DarwingOperation.BuildSolution] is OperationStates.Succeeded
            && _operations[DarwingOperation.TestUnmutatedSolution] is OperationStates.Succeeded
            && _operations[DarwingOperation.DiscoveringMutants] is OperationStates.Succeeded;
    }

    private bool CanTestMutants()
    {
        return _currentOperation is DarwingOperation.Idle
            && _operations[DarwingOperation.LoadSolution] is OperationStates.Succeeded
            && _operations[DarwingOperation.BuildSolution] is OperationStates.Succeeded
            && _operations[DarwingOperation.TestUnmutatedSolution] is OperationStates.Succeeded
            && _operations[DarwingOperation.DiscoveringMutants] is OperationStates.Succeeded
            && _operations[DarwingOperation.BuildingMutatedSolution] is OperationStates.Succeeded;
    }

}
