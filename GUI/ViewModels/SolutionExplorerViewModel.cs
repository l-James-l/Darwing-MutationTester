using GUI.ViewModels.SolutionExplorerElements;

namespace GUI.ViewModels;

public interface ISolutionExplorerViewModel
{

}

public class SolutionExplorerViewModel : ViewModelBase, ISolutionExplorerViewModel
{
    public SolutionExplorerViewModel(FileExplorerViewModel fileExplorerViewModel)
    {
        FileExplorerViewModel = fileExplorerViewModel;
    }

    public FileExplorerViewModel FileExplorerViewModel { get; }
}
