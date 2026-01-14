using GUI.Services;
using Models;
using Models.Events;

namespace GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IDashBoardViewModel _dashBoardViewModel;
    private readonly ISolutionExplorerViewModel _solutionExplorerViewModel;
    private readonly ISettingsViewModel _settingsViewModel;

    private readonly IFileSelectorService _fileSelectorService;
    private readonly IEventAggregator _eventAggregator;
    private readonly IMutationSettings _mutationSettings;

    /// <summary>
    /// The mainwindow will contain the main structure for the UI.
    /// Is responsible for managing naviagation between individual sub windows (namley the dashboard, solution explorer and settings page)
    /// </summary>
    public MainWindowViewModel(IFileSelectorService fileSelectorService, IEventAggregator eventAggregator,
        IMutationSettings mutationSettings, IDashBoardViewModel dashBoard, ISolutionExplorerViewModel slnExplorer,
        ISettingsViewModel settings)
    {
        // TODO DI these
        _dashBoardViewModel = dashBoard;
        _solutionExplorerViewModel = slnExplorer;
        _settingsViewModel = settings;
        
        _currentViewModel = default!; //Make the compilar happy. Setting the tab index will set this.
        SelectedTabIndex = 0;

        _fileSelectorService = fileSelectorService;
        _eventAggregator = eventAggregator;
        _mutationSettings = mutationSettings;

        SolutionPathSelection = new DelegateCommand(SelectSolutionPath);
        ReloadCurrentSolution = new DelegateCommand(ReloadCurrentSolutionCommand);
        RebuildCurrentSolution = new DelegateCommand(RebuildCurrentSolutionCommand);
        TestSolution = new DelegateCommand(TestSolutionCommand);
    }

    /// <summary>
    /// Set by the tab control in the view.
    /// Since the tab control bar, and content area are seperated in the layout, 
    /// we need to manually update the content area when a new tab is selected
    /// </summary>
    public int SelectedTabIndex
    {
        get => _selectedTabIndex;
        set
        {
            _selectedTabIndex = value;
            OnPropertyChanged();
            UpdateCurrentView();
        }
    }
    private int _selectedTabIndex;

    /// <summary>
    /// This is the view model actually displayed in the tab control content area.
    /// Should be set by updating the SelectedTabIndex
    /// </summary>
    public object CurrentViewModel
    {
        get => _currentViewModel;
        private set
        {
            _currentViewModel = value;
            OnPropertyChanged();
        }
    }
    private object _currentViewModel;

    private void UpdateCurrentView()
    {
        CurrentViewModel = SelectedTabIndex switch
        {
            1 => _solutionExplorerViewModel,
            2 => _settingsViewModel,
            _ => _dashBoardViewModel
        };
    }
    

    public DelegateCommand SolutionPathSelection { get; }
    private void SelectSolutionPath()
    {
        string? path = _fileSelectorService.OpenFileDialog("Solution Files (*.sln)|*.sln");
        if (!string.IsNullOrEmpty(path))
        {
            _eventAggregator.GetEvent<SolutionPathProvidedEvent>().Publish(new SolutionPathProvidedPayload(path));
        }
    }

    public DelegateCommand ReloadCurrentSolution { get; }
    private void ReloadCurrentSolutionCommand()
    {
        _eventAggregator.GetEvent<SolutionPathProvidedEvent>().Publish(new SolutionPathProvidedPayload(_mutationSettings.SolutionPath));
    }

    public DelegateCommand RebuildCurrentSolution { get; }
    private void RebuildCurrentSolutionCommand()
    {
        _eventAggregator.GetEvent<RequestSolutionBuildEvent>().Publish();
    }

    public DelegateCommand TestSolution { get; }
    private void TestSolutionCommand()
    {
        _eventAggregator.GetEvent<InitiateTestRunEvent>().Publish();
    }
}
