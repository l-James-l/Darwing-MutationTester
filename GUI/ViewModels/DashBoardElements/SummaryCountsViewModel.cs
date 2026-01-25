using Microsoft.CodeAnalysis;
using Models.Enums;
using Models.Events;
using Mutator;
using System.Windows.Media;

namespace GUI.ViewModels.DashBoardElements;

/// <summary>
/// View model for the row of overall stat cards on the dashboard
/// </summary>
public class SummaryCountsViewModel : ViewModelBase
{
    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationDiscoveryManager _mutationDiscoveryManager;

    public SummaryCountsViewModel(IEventAggregator eventAggregator, IMutationDiscoveryManager mutationDiscoveryManager)
    {
        _eventAggregator = eventAggregator;
        _mutationDiscoveryManager = mutationDiscoveryManager;

        InitializeStatCards();

        _eventAggregator.GetEvent<MutationUpdated>().Subscribe(_ => OnMutationUpdated(), ThreadOption.UIThread);
    }

    private void InitializeStatCards()
    {
        TotalMutationCount = new MutationStatCardViewModel
        {
            Title = "Total Mutations",
            Subtitle = "All discovered mutations",
            Value = 0,
            Icon = "🧪",
            IconBrush = Brushes.Orange
        };

        KilledMutationCount = new MutationStatCardViewModel
        {
            Title = "Killed",
            Subtitle = "Mutants killed by a failing test",
            Value = 0,
            Icon = "✔",
            IconBrush = Brushes.Green
        };

        SurvivedMutationCount = new MutationStatCardViewModel
        {
            Title = "Survived",
            Subtitle = "No test failed",
            Value = 0,
            Icon = "X",
            IconBrush = Brushes.Red
        };

        MutationScore = new MutationStatCardViewModel
        {
            Title = "Score",
            Subtitle = "Overall test score",
            Value = 0,
            ValueSuffix = "%",
            Icon = "🏆",
            IconBrush = Brushes.Blue
        };

    }


    public MutationStatCardViewModel TotalMutationCount
    { 
        get { return _totalMutationCount; }
        set
        {
            _totalMutationCount = value;
            OnPropertyChanged();
        }
    }
    private MutationStatCardViewModel _totalMutationCount = default!; // Make compiler happy, is set from ctor

    public MutationStatCardViewModel MutationScore
    {
        get { return _mutationScore; }
        set
        {
            _mutationScore = value;
            OnPropertyChanged();
        }
    }
    private MutationStatCardViewModel _mutationScore = default!; // Make compiler happy, is set from ctor

    public MutationStatCardViewModel KilledMutationCount
    {
        get { return _killedMutationCount; }
        set
        {
            _killedMutationCount = value;
            OnPropertyChanged();
        }
    }
    private MutationStatCardViewModel _killedMutationCount = new(); // Make compiler happy, is set from ctor

    public MutationStatCardViewModel SurvivedMutationCount
    {
        get { return _survivedMutationCount; }
        set
        {
            _survivedMutationCount = value;
            OnPropertyChanged();
        }
    }
    private MutationStatCardViewModel _survivedMutationCount = new(); // Make compiler happy, is set from ctor

    private void OnMutationUpdated()
    {
        TotalMutationCount.Value = _mutationDiscoveryManager.DiscoveredMutations.Count;
        KilledMutationCount.Value = _mutationDiscoveryManager.DiscoveredMutations.Count(x => x.Status is MutantStatus.Killed);
        SurvivedMutationCount.Value = _mutationDiscoveryManager.DiscoveredMutations.Count(x => x.Status is MutantStatus.Survived);
        int validCount = _mutationDiscoveryManager.DiscoveredMutations.Count(x => x.Status is not MutantStatus.CausedBuildError);
        if (validCount > 0)
        {
            MutationScore.Value = KilledMutationCount.Value*100/validCount;
        }
    }
}
