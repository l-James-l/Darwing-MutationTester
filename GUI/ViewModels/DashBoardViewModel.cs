using GUI.ViewModels.ElementViewModels;

namespace GUI.ViewModels;

public interface IDashBoardViewModel
{

}

public class DashBoardViewModel : ViewModelBase, IDashBoardViewModel
{
    public StatusBarViewModel StatusBarViewModel { get; set; }
    public MutationScoreByProjectViewModel MutationScoreByProjectViewModel { get; }

    public DashBoardViewModel(StatusBarViewModel statusBarViewModel, MutationScoreByProjectViewModel mutationScoreByProjectViewModel)
    {
        StatusBarViewModel = statusBarViewModel;
        MutationScoreByProjectViewModel = mutationScoreByProjectViewModel;
    }
}
