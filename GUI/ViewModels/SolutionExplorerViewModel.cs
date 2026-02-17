using GUI.Services;
using GUI.ViewModels.SolutionExplorerElements;
using Microsoft.CodeAnalysis;
using Models;
using Models.Enums;
using Models.Events;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace GUI.ViewModels;

public class SolutionExplorerViewModel : ViewModelBase
{
    private const string _defaultFileDisplayHeader = "No File Selected";
    private readonly IEventAggregator _eventAggregator;
    private readonly ISolutionProvider _solutionProvider;
    private FileNode? _selectedFileNode = null;

    public SolutionExplorerViewModel(FileExplorerViewModel fileExplorerViewModel, IEventAggregator eventAggregator, 
        ISolutionProvider solutionProvider)
    {
        FileExplorerViewModel = fileExplorerViewModel;
        _eventAggregator = eventAggregator;
        _solutionProvider = solutionProvider;

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
        if (_solutionProvider.SolutionContainer.FindFile(selectedFile.FullPath, ProjectType.Source) is { } file)
        {
            SelectedFileHeader = selectedFile.Name;
            _selectedFileNode = selectedFile;
            IEnumerable<string> lines = file.SyntaxTree.GetText().Lines.Select(x => x.ToString());
            List<LineDetails> lineDetails = [.. lines.Select((line, index) => new LineDetails
            {
                SourceCode = line,
                LineNumber = index + 1,
                MutationsOnLine = [.. selectedFile.MutationInFile.Where(x => x.LineSpan.StartLinePosition.Line == index && x.Status.IncludeInReport())],
                IsChecked = file.LinesToMutate.ContainsLine(index)
            })];

            FileDetails.AddRange(lineDetails);

            //Set the callback after so that initialization doesn't mess with included lines.
            foreach (LineDetails line in FileDetails)
            {
                line.ToggleLineInclusion = (int lineNo, bool include) => ToggleLineInclusion(lineNo, include, file.LinesToMutate);
                
            }

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

    private void ToggleLineInclusion(int lineNo, bool include, FileLineCollection lineCollection)
    {
        if (include)
        {
            lineCollection.Add(lineNo);
        }
        else
        {
            lineCollection.Remove(lineNo);
        }

        if (_selectedFileNode is null)
        {
            return;
        }
        if (!_selectedFileNode.IsChecked && lineCollection.Any())
        {
            _selectedFileNode.NotifyCheckedFromLineInFile(true);
        }
        else if (_selectedFileNode.IsChecked && !lineCollection.Any())
        {
            _selectedFileNode.NotifyCheckedFromLineInFile(false);
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

    public bool IsChecked 
    { 
        get; 
        set 
        {
            field = value;
            ToggleLineInclusion?.Invoke(LineNumber - 1, value);
        } 
    }

    public Action<int, bool>? ToggleLineInclusion { private get; set; }
}