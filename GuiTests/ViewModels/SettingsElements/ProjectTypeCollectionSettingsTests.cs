using GUI.ViewModels.SettingsElements;
using Models;
using Models.Enums;
using Models.Events;
using NSubstitute;

namespace GuiTests.ViewModels.SettingsElements;

public class ProjectTypeCollectionSettingsTests
{
    private IEventAggregator _eventAggregator;
    private ISolutionProvider _solutionProvider;
    private IMutationSettings _settings;
    private SettingChanged _settingChangedEvent;

    // We need to capture the refresh action
    private Action<DarwingOperation> _onRefreshAction;
    private ProjectTypeCollectionSettings _sut;

    [SetUp]
    public void Setup()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _settings = Substitute.For<IMutationSettings>();
        _settingChangedEvent = Substitute.For<SettingChanged>();

        _eventAggregator.GetEvent<SettingChanged>().Returns(_settingChangedEvent);

        // Capture the subscription callback
        var refreshEvent = Substitute.For<DarwingOperationStatesChangedEvent>();
        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Returns(refreshEvent);

        // When Subscribe is called, save the action passed to it
        refreshEvent.When(x => x.Subscribe(
            Arg.Any<Action<DarwingOperation>>(),
            Arg.Any<ThreadOption>(),
            Arg.Any<bool>(),
            Arg.Any<Predicate<DarwingOperation>>()))
            .Do(info => _onRefreshAction = info.Arg<Action<DarwingOperation>>());

        _settings.SourceCodeProjects.Returns(new List<string>());
        _settings.TestProjects.Returns(new List<string>());
        _settings.IgnoreProjects.Returns(new List<string>());

        _sut = new ProjectTypeCollectionSettings(_eventAggregator, _solutionProvider, _settings);
    }

    [Test]
    public void GivenProjectExists_WhenTypeChangedToTest_ThenCollectionsAreUpdated()
    {
        // Arrange
        var projectName = "CoreLib";
        var projectMock = CreateProjectMock(projectName, ProjectType.Source);
        _settings.SourceCodeProjects.Add(projectName);

        // Manually invoke the captured subscription to "Load" the projects
        SetupSolutionMock(projectMock);
        _onRefreshAction.Invoke(DarwingOperation.LoadSolution);

        var projectVm = _sut.Projects.First();

        // Act
        projectVm.Type = ProjectType.Test;

        // Assert
        Assert.That(_settings.TestProjects, Contains.Item(projectName));
        Assert.That(_settings.SourceCodeProjects, Does.Not.Contain(projectName));
        _settingChangedEvent.Received(1).Publish(nameof(IMutationSettings.TestProjects));
    }

    private IProjectContainer CreateProjectMock(string name, ProjectType type)
    {
        var mock = Substitute.For<IProjectContainer>();
        mock.Name.Returns(name);
        mock.ProjectType = type;
        return mock;
    }

    private void SetupSolutionMock(params IProjectContainer[] projects)
    {
        var container = Substitute.For<ISolutionContainer>();
        container.AllProjects.Returns(projects.ToList());
        _solutionProvider.IsAvailable.Returns(true);
        _solutionProvider.SolutionContainer.Returns(container);
    }
}