using GUI.Services;
using GUI.ViewModels.SolutionExplorerElements;
using Microsoft.CodeAnalysis;
using Models;
using Models.Enums;
using Models.Events;
using System.Collections.ObjectModel;
using System.IO;

namespace GUI.ViewModels;

public interface ISolutionExplorerViewModel
{

}

public class SolutionExplorerViewModel : ViewModelBase, ISolutionExplorerViewModel
{
    private const string _defaultFileDisplayHeader = "No File Selected";
    private readonly IEventAggregator _eventAggregator;
    
    private FileNode? _selectedFileNode = null;

    public SolutionExplorerViewModel(FileExplorerViewModel fileExplorerViewModel, IEventAggregator eventAggregator)
    {
        FileExplorerViewModel = fileExplorerViewModel;
        _eventAggregator = eventAggregator;

        fileExplorerViewModel.SelectedFileChangedCallBack += OnSelectedFileChanged;

        _eventAggregator.GetEvent<MutationUpdated>().Subscribe(_ => OnPropertyChanged(nameof(SelectedLine)), ThreadOption.UIThread, true, 
            x => SelectedLine is not null && SelectedLine.MutationsOnLine.Any(m => m.ID == x));
    }

    /// <summary>
    /// View model responsible for the file tree, and exposing the currently selected file.
    /// </summary>
    public FileExplorerViewModel FileExplorerViewModel { get; }

    /// <summary>
    /// Binding property for the name of the selected file, or if one is not selected, a string indicating that.
    /// </summary>
    public string SelectedFileHeader 
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

    public LineDetails? SelectedLine
    {
        get;
        set => SetProperty(ref field, value);
    } = null;
    
    private void OnSelectedFileChanged(FileNode selectedFile)
    {
        //If the same file is selected, try to keep the same line selected. Any expanded mutations will be lost, but that's acceptable.
        int selectedLineNumber = -1;
        if (selectedFile == _selectedFileNode && SelectedLine is not null)
        {
            selectedLineNumber = SelectedLine.LineNumber;
        }

        SelectedLine = null;
        FileDetails.Clear();        
        if (File.Exists(selectedFile.FullPath))
        {
            SelectedFileHeader = selectedFile.Name;
            _selectedFileNode = selectedFile;
            IEnumerable<string> lines = File.ReadLines(selectedFile.FullPath);
            List<LineDetails> lineDetails = [.. lines.Select((line, index) => new LineDetails
            {
                SourceCode = line,
                LineNumber = index + 1,
                MutationsOnLine = [.. selectedFile.MutationInFile.Where(x => x.LineSpan.StartLinePosition.Line == index && x.Status is not MutantStatus.CausedBuildError)]
            })];

            FileDetails.AddRange(lineDetails);

            if (selectedLineNumber > -1)
            {
                SelectedLine = FileDetails.FirstOrDefault(x => x.LineNumber == selectedLineNumber);
            }
        }
        else
        {
            SelectedFileHeader = _defaultFileDisplayHeader;
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

    public ObservableCollection<DiscoveredMutation> MutationsOnLine { get; set; } = [];
}