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
        Name = proj.Name;
        ID = proj.ID;
    }

    /// <summary>
    /// Not displayed on UI so doesn't invoke OnPropertyChanged.
    /// </summary>
    public ProjectId ID {  get; set; }

    public string Name
    {
        get; 
        set => SetProperty(ref field, value);
    }

    public int TotalMutations
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (TotalMutations > 0)
            {
                MutationScore = KilledMutations*100 / TotalMutations;
            }
        }
    }

    public int KilledMutations
    {
        get;
        set
        {
            SetProperty(ref field, value);
            if (TotalMutations > 0)
            {
                MutationScore = KilledMutations*100 / TotalMutations;
            }
        }
    }

    public int SurvivedMutations
    {
        get; 
        set => SetProperty(ref field, value);
    }

    public int MutationScore
    {
        get; 
        set => SetProperty(ref field, value);
    }
}