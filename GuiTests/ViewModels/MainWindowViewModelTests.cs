using NUnit.Framework;
using NSubstitute;
using GUI.ViewModels;
using GUI.Services;
using Core.Interfaces;
using Models;
using Mutator;

namespace GUITests.ViewModels;

public class MainWindowViewModelTests
{
    private IFileSelectorService _fileSelector;
    private ISolutionLoader _solutionLoader;
    private IMutationSettings _mutationSettings;
    private IDashBoardViewModel _dashBoard;
    private ISolutionExplorerViewModel _slnExplorer;
    private ISettingsViewModel _settings;
    private ISolutionBuilder _solutionBuilder;
    private IMutationRunInitiator _mutationRunInitiator;
    private IConsoleService _consoleService;

    private MainWindowViewModel _mainWindowVm;

    [SetUp]
    public void SetUp()
    {
        _fileSelector = Substitute.For<IFileSelectorService>();
        _solutionLoader = Substitute.For<ISolutionLoader>();
        _mutationSettings = Substitute.For<IMutationSettings>();
        _dashBoard = Substitute.For<IDashBoardViewModel>();
        _slnExplorer = Substitute.For<ISolutionExplorerViewModel>();
        _settings = Substitute.For<ISettingsViewModel>();
        _solutionBuilder = Substitute.For<ISolutionBuilder>();
        _mutationRunInitiator = Substitute.For<IMutationRunInitiator>();
        _consoleService = Substitute.For<IConsoleService>();

        _mainWindowVm = new MainWindowViewModel(
            _fileSelector, _solutionLoader, _mutationSettings,
            _dashBoard, _slnExplorer, _settings,
            _solutionBuilder, _mutationRunInitiator, _consoleService);
    }

    [Test]
    public void GivenDefaultState_WhenViewModelCreated_ThenSelectedTabIsDashboard()
    {
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_mainWindowVm.SelectedTabIndex, Is.EqualTo(0));
            Assert.That(_mainWindowVm.CurrentViewModel, Is.SameAs(_dashBoard));
        });
    }

    [Test]
    [TestCase(1, typeof(ISolutionExplorerViewModel))]
    [TestCase(2, typeof(ISettingsViewModel))]
    [TestCase(0, typeof(IDashBoardViewModel))]
    [TestCase(99, typeof(IDashBoardViewModel))] // Testing default/fallback case
    public void GivenNewTabIndex_WhenSelectedTabIndexChanges_ThenCurrentViewModelUpdates(int index, Type expectedType)
    {
        // Act
        _mainWindowVm.SelectedTabIndex = index;

        // Assert
        Assert.That(_mainWindowVm.CurrentViewModel, Is.InstanceOf(expectedType));
    }

    [Test]
    public void GivenValidPathSelected_WhenSolutionPathSelectionExecuted_ThenSolutionLoaderIsCalled()
    {
        // Arrange
        const string expectedPath = @"C:\Repo\MySolution.sln";
        _fileSelector.OpenFileDialog(Arg.Any<string>()).Returns(expectedPath);

        // Act
        _mainWindowVm.SolutionPathSelection.Execute();

        // Assert
        Thread.Sleep(100);
        _solutionLoader.Received(1).Load(expectedPath);
    }

    [Test]
    public void GivenExistingSolution_WhenReloadCurrentSolutionExecuted_ThenLoaderUsesSettingsPath()
    {
        // Arrange
        _mutationSettings.SolutionPath.Returns(@"C:\Old\Path.sln");

        // Act
        _mainWindowVm.ReloadCurrentSolution.Execute();

        // Assert
        Thread.Sleep(100);
        _solutionLoader.Received(1).Load(@"C:\Old\Path.sln");
    }

    [Test]
    public void WhenRebuildCurrentSolutionExecuted_ThenSolutionBuilderIsCalled()
    {
        // Act
        _mainWindowVm.RebuildCurrentSolution.Execute();

        // Assert
        Thread.Sleep(100);
        _solutionBuilder.Received(1).InitialBuild();
    }

    [Test]
    public void WhenTestSolutionExecuted_ThenMutationInitiatorIsCalled()
    {
        // Act
        _mainWindowVm.TestSolution.Execute();

        // Assert
        Thread.Sleep(100);
        _mutationRunInitiator.Received(1).Run();
    }

    [Test]
    public void WhenToggleConsoleExecuted_ThenConsoleServiceTogglesVisibility()
    {
        // Act
        _mainWindowVm.ToggleConsole.Execute();

        // Assert
        Thread.Sleep(100);
        _consoleService.Received(1).ToggleConsoleVisable();
    }
}