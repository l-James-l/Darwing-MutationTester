using Core.Interfaces;
using GUI.Services;
using Models;
using Mutator;

namespace GUI.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IDashBoardViewModel _dashBoardViewModel;
    private readonly ISolutionExplorerViewModel _solutionExplorerViewModel;
    private readonly ISettingsViewModel _settingsViewModel;
    private readonly ISolutionBuilder _solutionBuilder;
    private readonly IMutationRunInitiator _mutationRunInitiator;
    private readonly IFileSelectorService _fileSelectorService;
    private readonly ISolutionLoader _solutionLoader;
    private readonly IMutationSettings _mutationSettings;

    /// <summary>
    /// The main window will contain the main structure for the UI.
    /// Is responsible for managing navigation between individual sub windows (namely the dashboard, solution explorer and settings page)
    /// </summary>
    public MainWindowViewModel(IFileSelectorService fileSelectorService, ISolutionLoader solutionLoader,
        IMutationSettings mutationSettings, IDashBoardViewModel dashBoard, ISolutionExplorerViewModel slnExplorer,
        ISettingsViewModel settings, ISolutionBuilder solutionBuilder, IMutationRunInitiator mutationRunInitiator)
    {
        ArgumentNullException.ThrowIfNull(fileSelectorService);
        ArgumentNullException.ThrowIfNull(solutionLoader);
        ArgumentNullException.ThrowIfNull(mutationSettings);
        ArgumentNullException.ThrowIfNull(dashBoard);
        ArgumentNullException.ThrowIfNull(slnExplorer);
        ArgumentNullException.ThrowIfNull(settings);
        ArgumentNullException.ThrowIfNull(solutionBuilder);
        ArgumentNullException.ThrowIfNull(mutationRunInitiator);

        _dashBoardViewModel = dashBoard;
        _solutionExplorerViewModel = slnExplorer;
        _settingsViewModel = settings;

        SelectedTabIndex = 0;
        CurrentViewModel = _dashBoardViewModel;

        _fileSelectorService = fileSelectorService;
        _solutionLoader = solutionLoader;
        _mutationSettings = mutationSettings;
        _solutionBuilder = solutionBuilder;
        _mutationRunInitiator = mutationRunInitiator;

        SolutionPathSelection = new DelegateCommand(SelectSolutionPath);
        ReloadCurrentSolution = new DelegateCommand(ReloadCurrentSolutionCommand);
        RebuildCurrentSolution = new DelegateCommand(RebuildCurrentSolutionCommand);
        TestSolution = new DelegateCommand(TestSolutionCommand);
    }

    /// <summary>
    /// Set by the tab control in the view.
    /// Since the tab control bar, and content area are separated in the layout, 
    /// we need to manually update the content area when a new tab is selected
    /// </summary>
    public int SelectedTabIndex
    {
        get;
        set
        {
            SetProperty(ref field, value);
            UpdateCurrentView();
        }
    }

    /// <summary>
    /// This is the view model actually displayed in the tab control content area.
    /// Should be set by updating the SelectedTabIndex
    /// </summary>
    public object CurrentViewModel
    {
        get; 
        private set => SetProperty(ref field, value);
    }

    private void UpdateCurrentView()
    {
        CurrentViewModel = SelectedTabIndex switch
        {
            1 => _solutionExplorerViewModel,
            2 => _settingsViewModel,
            _ => _dashBoardViewModel
        };
    }

    // Important: The following commands are async void on purpose.
    // They are triggered by UI interactions and need to run asynchronously without blocking the UI thread.
    // Note we don't do any validation or error handling here - that is handled in the services called.

    public DelegateCommand SolutionPathSelection { get; }
    private async void SelectSolutionPath()
    {
        string? path = _fileSelectorService.OpenFileDialog("Solution Files (*.sln)|*.sln");
        if (!string.IsNullOrEmpty(path))
        {
            await Task.Run(() => _solutionLoader.Load(path));
        }
    }

    public DelegateCommand ReloadCurrentSolution { get; }
    private async void ReloadCurrentSolutionCommand()
    {
        await Task.Run(() => _solutionLoader.Load(_mutationSettings.SolutionPath));
    }

    public DelegateCommand RebuildCurrentSolution { get; }
    private async void RebuildCurrentSolutionCommand()
    {
        await Task.Run(_solutionBuilder.InitialBuild);
    }

    public DelegateCommand TestSolution { get; }
    private async void TestSolutionCommand()
    {
        await Task.Run(_mutationRunInitiator.Run);
    }
}
