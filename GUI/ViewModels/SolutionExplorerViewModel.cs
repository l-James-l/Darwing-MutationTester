using Core.Interfaces;
using GUI.Services;
using GUI.ViewModels.SolutionExplorerElements;
using Microsoft.CodeAnalysis;
using Models;
using Models.Enums;
using Models.Events;
using System.Collections.ObjectModel;
using System.Windows;

namespace GUI.ViewModels;

public class SolutionExplorerViewModel : ViewModelBase
{
    private const string _defaultFileDisplayHeader = "No File Selected";
    private const string _defactoFullSolutionTestHeader = "Test full solution";

    private readonly IEventAggregator _eventAggregator;
    private readonly ISolutionProvider _solutionProvider;
    private readonly IGitDiffManager _gitManager;
    private readonly IGeminiApiHandler _geminiApi;

    private FileNode? _selectedFileNode = null;

    public SolutionExplorerViewModel(FileExplorerViewModel fileExplorerViewModel, IEventAggregator eventAggregator, 
        ISolutionProvider solutionProvider, IGitDiffManager gitManager, IGeminiApiHandler geminiApi)
    {
        FileExplorerViewModel = fileExplorerViewModel;
        _eventAggregator = eventAggregator;
        _solutionProvider = solutionProvider;
        _gitManager = gitManager;
        _geminiApi = geminiApi;

        fileExplorerViewModel.SelectedFileChangedCallBack += OnSelectedFileChanged;
        TryGetUnitTestCommand = new DelegateCommand<MutationViewModel>(TryGetUnitTest);

        _eventAggregator.GetEvent<MutationUpdated>().Subscribe(_ => OnPropertyChanged(nameof(SelectedLine)), ThreadOption.UIThread, true, 
            x => SelectedLine is not null && SelectedLine.MutationsOnLine.Any(m => m.Mutation.ID == x));
        _eventAggregator.GetEvent<GitUpdateEvent>().Subscribe(OnGitUpdateEvent, ThreadOption.UIThread);
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

    /// <summary>
    /// Binding property for the currently selected line in the file, 
    /// which is used to display the mutations on that line in the details pane.
    /// </summary>
    public LineDetails? SelectedLine
    {
        get;
        set => SetProperty(ref field, value);
    } = null;

    /// <summary>
    /// Binding property for the visibility of the git branch selection dropdown. 
    /// This is visible when a git repository is detected at the solution path, and hidden otherwise.
    /// </summary>
    public Visibility GitVisibility 
    {
        get;
        private set
        {
            SetProperty(ref field, value);
        }
    } = Visibility.Hidden;

    /// <summary>
    /// Binding property for the list of git branches available to compare against. 
    /// This is populated when a git repository is detected at the solution path, and is empty otherwise.
    /// </summary>
    public List<string> AvailableGitBranches 
    {
        get; 
        set
        {
            SetProperty(ref field, value);
        } 
    } = [];

    /// <summary>
    /// Binding property for the currently selected git branch to compare against.
    /// When this is set, the git manager will establish the diff, which will update the files/lines to mutate in the solution container.
    /// </summary>
    public string SelectedBranch
    {
        get => _selectedBranch;
        set
        {
            if (value == _defactoFullSolutionTestHeader)
            {
                _solutionProvider.SolutionContainer.SolutionProjects.ForEach(x => x.FileCollection.ForEach(y => y.LinesToMutate.Full()));
                OnPropertyChanged(nameof(FileDetails));
                FileExplorerViewModel.UpdateCheckedStates(FileExplorerViewModel.SolutionTree);
            }
            else 
            {
                SetProperty(ref _selectedBranch, value);
                _gitManager.EstablishDiff(value);
            }
        }
    } 
    private string _selectedBranch = "";

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
                MutationsOnLine = [.. selectedFile.MutationInFile.Where(x => x.LineSpan.StartLinePosition.Line == index && x.Status.IncludeInReport()).Select(x => new MutationViewModel(x, _geminiApi.IsConfigured))],
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

    private void OnGitUpdateEvent()
    {
        //Handle updates to checked lines in the selected file
        OnPropertyChanged(nameof(FileDetails));

        //Insert the constant option to test the full solution, this should always be at the top.
        AvailableGitBranches = _gitManager.Branches;
        GitVisibility = _gitManager.Branches.Count > 0 ? Visibility.Visible : Visibility.Hidden;
        AvailableGitBranches.Insert(0, _defactoFullSolutionTestHeader);

        //Set the backing field directly to avoid triggering the diff again, as the git manager will have already established the diff by the time this event is triggered.
        _selectedBranch = _gitManager.LastSelectedBranch ?? _defactoFullSolutionTestHeader;
        OnPropertyChanged(nameof(SelectedBranch));
    }

    /// <summary>
    /// Binding command for the button to generate a suggested test for a failed mutation.
    /// </summary>
    public DelegateCommand<MutationViewModel> TryGetUnitTestCommand { get; }
    private async void TryGetUnitTest(MutationViewModel mutation)
    {
        mutation.TestGenerationOngoingVisibility = Visibility.Visible;
        await _geminiApi.GenerateUnitTest(mutation.Mutation, mutation.MutationTestCreatedCallBack);
        mutation.TestGenerationOngoingVisibility = Visibility.Collapsed;
    }
}

/// <summary>
/// Data class for representing a single line in the selected file.
/// </summary>
public class LineDetails
{
    public string SourceCode { get; set; } = "";

    public int LineNumber { get; set; } = -1;

    public ObservableCollection<MutationViewModel> MutationsOnLine { get; set; } = [];

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
