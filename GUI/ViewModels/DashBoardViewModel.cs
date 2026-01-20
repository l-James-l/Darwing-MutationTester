using GUI.ViewModels.ElementViewModels;

namespace GUI.ViewModels;

public interface IDashBoardViewModel
{

}

public class DashBoardViewModel : ViewModelBase, IDashBoardViewModel
{
    public StatusBarViewModel StatusBarViewModel { get; set; }

    public DashBoardViewModel(StatusBarViewModel statusBarViewModel)
    {
        StatusBarViewModel = statusBarViewModel;
    }
}
