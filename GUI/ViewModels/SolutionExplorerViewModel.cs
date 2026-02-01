using GUI.Services;
using GUI.ViewModels.SolutionExplorerElements;
using System.Collections.ObjectModel;
using System.IO;

namespace GUI.ViewModels;

public interface ISolutionExplorerViewModel
{

}

public class SolutionExplorerViewModel : ViewModelBase, ISolutionExplorerViewModel
{
    private const string _defaultFileDisplayHeader = "No File Selected";

    public SolutionExplorerViewModel(FileExplorerViewModel fileExplorerViewModel)
    {
        FileExplorerViewModel = fileExplorerViewModel;

        fileExplorerViewModel.SelectedFileChangedCallBack += OnSelectedFileChanged;
    }

    /// <summary>
    /// View model responsible for the file tree, and exposing the currently selected file.
    /// </summary>
    public FileExplorerViewModel FileExplorerViewModel { get; }

    /// <summary>
    /// Binding property for the name of the selected file.
    /// </summary>
    public string SelectedFileName 
    { 
        get; 
        set => SetProperty(ref field, value); 
    } = _defaultFileDisplayHeader;

    /// <summary>
    /// Binding property for the contents of the selected file.
    /// We use a collection rather than just the string containing all the file content so that we can control the
    /// line styling, and show on a line by line basis what mutations are in the file
    /// </summary>
    public ObservableCollection<LineDetails> FileDetails { get; } = [];

    private void OnSelectedFileChanged(FileNode selectedFile)
    {
        FileDetails.Clear();
        string newFilePath = selectedFile.FullPath;
        if (File.Exists(newFilePath))
        {
            SelectedFileName = selectedFile.Name;
            IEnumerable<string> lines = File.ReadLines(newFilePath);
            List<LineDetails> lineDetails = [.. lines.Select((line, index) => new LineDetails
            {
                SourceCode = line,
                LineNumber = index + 1
            })];

            FileDetails.AddRange(lineDetails);
        }
        else
        {
            SelectedFileName = _defaultFileDisplayHeader;
        }
    }
}

/// <summary>
/// Data class for representing a single line in the selected file.
/// </summary>
public class LineDetails
{
    public string SourceCode { get; set; } = "";

    public int LineNumber { get; set; } = -1;
}