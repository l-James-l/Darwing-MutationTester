using GUI.ViewModels.SettingsElements;
using Models;
using Models.Enums;
using Models.Events;
using NSubstitute;

namespace GuiTests.ViewModels.SettingsElements;

public class GeneralSettingsViewModelTests
{
    private IMutationSettings _settings;
    private IEventAggregator _eventAggregator;

    private Action<DarwingOperation> _onRefreshAction;
    private GeneralSettingsViewModel _sut;

    [SetUp]
    public void Setup()
    {
        // Arrange
        _settings = Substitute.For<IMutationSettings>();
        _eventAggregator = Substitute.For<IEventAggregator>();

        // Set initial dummy values in the settings model
        _settings.BuildTimeout.Returns(100);
        _settings.TestRunTimeout.Returns(200);
        _settings.SingleMutantPerLine.Returns(true);
        _settings.SkipTestingNoActiveMutants.Returns(false);

        // Capture the Event Aggregator subscription
        var refreshEvent = Substitute.For<DarwingOperationStatesChangedEvent>();
        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Returns(refreshEvent);

        refreshEvent.When(x => x.Subscribe(
            Arg.Any<Action<DarwingOperation>>(),
            Arg.Any<ThreadOption>(),
            Arg.Any<bool>(),
            Arg.Any<Predicate<DarwingOperation>>()))
            .Do(info => _onRefreshAction = info.Arg<Action<DarwingOperation>>());

        _sut = new GeneralSettingsViewModel(_settings, _eventAggregator);
    }

    [Test]
    public void GivenViewModelInitialized_WhenCreated_ThenPropertiesMatchSettings()
    {
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_sut.BuildTimeout, Is.EqualTo(100));
            Assert.That(_sut.TestTimeout, Is.EqualTo(200));
            Assert.That(_sut.SingleMutationPerLine, Is.True);
            Assert.That(_sut.SkipTestingNoActiveMutants, Is.False);
        });
    }

    [Test]
    public void GivenBuildTimeoutChanged_WhenUserSetsValue_ThenSettingsModelIsUpdated()
    {
        // Act
        _sut.BuildTimeout = 500;

        // Assert
        _settings.Received(1).BuildTimeout = 500;
    }

    [Test]
    public void GivenSingleMutationChanged_WhenUserToggles_ThenSettingsModelIsUpdated()
    {
        // Act
        _sut.SingleMutationPerLine = false;

        // Assert
        _settings.Received(1).SingleMutantPerLine = false;
    }

    [Test]
    public void GivenSettingsChangedInBackground_WhenLoadSolutionEventFired_ThenViewModelRefreshes()
    {
        // Arrange
        // Update the mock settings to simulate a new profile being loaded
        _settings.BuildTimeout.Returns(999);
        _settings.TestRunTimeout.Returns(888);

        // Act
        // Manually trigger the captured callback
        _onRefreshAction.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(_sut.BuildTimeout, Is.EqualTo(999));
            Assert.That(_sut.TestTimeout, Is.EqualTo(888));
        });
    }
}