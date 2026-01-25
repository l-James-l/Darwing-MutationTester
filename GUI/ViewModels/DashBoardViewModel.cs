using GUI.ViewModels.DashBoardElements;

namespace GUI.ViewModels;

public interface IDashBoardViewModel
{

}

public class DashBoardViewModel : ViewModelBase, IDashBoardViewModel
{
    public StatusBarViewModel StatusBarViewModel { get; set; }
    public MutationScoreByProjectViewModel MutationScoreByProjectViewModel { get; }
    public SummaryCountsViewModel SummaryCountsViewModel { get; }

    public DashBoardViewModel(StatusBarViewModel statusBarViewModel, MutationScoreByProjectViewModel mutationScoreByProjectViewModel,
        SummaryCountsViewModel summaryCountsViewModel)
    {
        StatusBarViewModel = statusBarViewModel;
        MutationScoreByProjectViewModel = mutationScoreByProjectViewModel;
        SummaryCountsViewModel = summaryCountsViewModel;
    }
}
