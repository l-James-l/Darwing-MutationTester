using Microsoft.CodeAnalysis;
using Models;

namespace GUI.ViewModels.DashBoardElements;

/// <summary>
/// This class is used to represent an individual row on the dashboard UI, in the project summary section.
/// So will contain summary info on a per project basis
/// </summary>
public class IndividualProjectSummaryViewModel : ViewModelBase
{

    public IndividualProjectSummaryViewModel(IProjectContainer proj)
    {
        _name = proj.Name;
        ID = proj.ID;
    }

    /// <summary>
    /// Not displayed on UI so doesn't invoke OnPropertyChanged.
    /// </summary>
    public ProjectId ID {  get; set; }

    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            OnPropertyChanged();
        }
    }
    private string _name;

    public int TotalMutations
    {
        get => _totalMutations;
        set
        {
            _totalMutations = value;
            OnPropertyChanged();
            if (_totalMutations > 0)
            {
                MutationScore = _killedMutations*100 / _totalMutations;
            }
        }
    }
    private int _totalMutations = 0;

    public int KilledMutations
    {
        get => _killedMutations;
        set
        {
            _killedMutations = value;
            OnPropertyChanged();
            if (_totalMutations > 0)
            {
                MutationScore = _killedMutations*100 / _totalMutations;
            }
        }
    }
    private int _killedMutations = 0;

    public int SurvivedMutations
    {
        get => _survivedMutations;
        set
        {
            _survivedMutations = value;
            OnPropertyChanged();
        }
    }
    private int _survivedMutations = 0;

    public int MutationScore
    {
        get => _mutationScore;
        set
        {
            _mutationScore = value;
            OnPropertyChanged();
        }
    }
    private int _mutationScore = 0;
}