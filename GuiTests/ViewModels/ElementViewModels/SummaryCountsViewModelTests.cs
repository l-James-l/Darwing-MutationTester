using Models.Enums;
using Models.Events;
using Mutator;
using GUI.ViewModels.DashBoardElements;
using NSubstitute;
using Microsoft.CodeAnalysis;
using Models;
using Microsoft.CodeAnalysis.CSharp;

namespace GuiTests.ViewModels.ElementViewModels;

public class SummaryCountsViewModelTests
{
    private IEventAggregator _eventAggregator;
    private IMutationDiscoveryManager _mutationDiscoveryManager;
    private MutationUpdated _mutationUpdatedEvent;
    private Action<SyntaxAnnotation> _updateCallback = null;

    [SetUp]
    public void SetUp()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _mutationDiscoveryManager = Substitute.For<IMutationDiscoveryManager>();

        _mutationUpdatedEvent = Substitute.For<MutationUpdated>();
        _eventAggregator.GetEvent<MutationUpdated>().Returns(_mutationUpdatedEvent);

        // Since the subscription is on the ui thread, we need to capture and manually activate the callback.
        // Note: the value of the SyntaxAnnotation is ignored so we can just give default.
        _mutationUpdatedEvent.When(x => x.Subscribe(Arg.Any<Action<SyntaxAnnotation>>(), ThreadOption.UIThread))
            .Do(x => _updateCallback = x.Arg<Action<SyntaxAnnotation>>());
    }

    public void GivenConstructed_ThenSubscriptionMade()
    {
        //Arrange + Act
        var vm = new SummaryCountsViewModel(_eventAggregator, _mutationDiscoveryManager);
        
        //Assert
        Assert.That(_updateCallback, Is.Not.Null);
    }

    [Test]
    public void GivenViewModelConstructed_ThenStatCardsAreCreatedWithCorrectDefaults()
    {
        // Arrange
        _mutationDiscoveryManager.DiscoveredMutations.Returns([]);

        // Act
        var vm = new SummaryCountsViewModel(_eventAggregator, _mutationDiscoveryManager);

        // Assert
        Assert.That(vm.TotalMutationCount.Title, Is.EqualTo("Total Mutations"));
        Assert.That(vm.KilledMutationCount.Title, Is.EqualTo("Killed"));
        Assert.That(vm.SurvivedMutationCount.Title, Is.EqualTo("Survived"));
        Assert.That(vm.MutationScore.Title, Is.EqualTo("Score"));

        Assert.That(vm.TotalMutationCount.Value, Is.Zero);
        Assert.That(vm.KilledMutationCount.Value, Is.EqualTo(0));
        Assert.That(vm.SurvivedMutationCount.Value, Is.EqualTo(0));
        Assert.That(vm.MutationScore.Value, Is.EqualTo(0));
    }

    [Test]
    public void GivenNoMutations_WhenMutationUpdatedEventRaised_ThenAllCountsRemainZero()
    {
        // Arrange
        _mutationDiscoveryManager.DiscoveredMutations.Returns([]);
        var vm = new SummaryCountsViewModel(_eventAggregator, _mutationDiscoveryManager);

        // Act
        _updateCallback.Invoke(default);

        // Assert
        Assert.That(vm.TotalMutationCount.Value, Is.EqualTo(0));
        Assert.That(vm.KilledMutationCount.Value, Is.Zero);
        Assert.That(vm.SurvivedMutationCount.Value, Is.Zero);
        Assert.That(vm.MutationScore.Value, Is.Zero);
    }

    [Test]
    public void GivenMixedMutations_WhenMutationUpdatedEventRaised_ThenCountsAreCalculatedCorrectly()
    {
        // Arrange
        var mutations = new List<DiscoveredMutation>
        {
            CreateMutation(MutantStatus.Killed),
            CreateMutation(MutantStatus.Killed),
            CreateMutation(MutantStatus.Survived),
            CreateMutation(MutantStatus.Survived)
        };

        _mutationDiscoveryManager.DiscoveredMutations.Returns(mutations);

        var vm = new SummaryCountsViewModel(_eventAggregator, _mutationDiscoveryManager);

        // Act
        _updateCallback.Invoke(default);

        // Assert
        Assert.That(vm.TotalMutationCount.Value, Is.EqualTo(4));
        Assert.That(vm.KilledMutationCount.Value, Is.EqualTo(2));
        Assert.That(vm.SurvivedMutationCount.Value, Is.EqualTo(2));

        // validCount = 4, killed = 2 → 2 * 100 / 4 = 50
        Assert.That(vm.MutationScore.Value, Is.EqualTo(50));
    }

    [Test]
    public void GivenBuildErrorMutations_WhenMutationUpdatedEventRaised_ThenTheyAreExcludedFromScoreCalculation()
    {
        // Arrange
        var mutations = new List<DiscoveredMutation>
        {
            CreateMutation(MutantStatus.Killed),
            CreateMutation(MutantStatus.Killed),
            CreateMutation(MutantStatus.CausedBuildError),
            CreateMutation(MutantStatus.CausedBuildError)
        };

        _mutationDiscoveryManager.DiscoveredMutations.Returns(mutations);

        var vm = new SummaryCountsViewModel(_eventAggregator, _mutationDiscoveryManager);

        // Act
        _updateCallback.Invoke(default);

        // Assert
        Assert.That(vm.TotalMutationCount.Value, Is.EqualTo(4));
        Assert.That(vm.KilledMutationCount.Value, Is.EqualTo(2));
        Assert.That(vm.SurvivedMutationCount.Value, Is.Zero);

        // validCount = 2 (build errors excluded)
        // score = 2 * 100 / 2 = 100
        Assert.That(vm.MutationScore.Value, Is.EqualTo(100));
    }

    [Test]
    public void GivenOnlyBuildErrors_WhenMutationUpdatedEventRaised_ThenScoreIsNotUpdated()
    {
        // Arrange
        var mutations = new List<DiscoveredMutation>
        {
            CreateMutation(MutantStatus.CausedBuildError),
            CreateMutation(MutantStatus.CausedBuildError)
        };

        _mutationDiscoveryManager.DiscoveredMutations.Returns(mutations);

        var vm = new SummaryCountsViewModel(_eventAggregator, _mutationDiscoveryManager);

        // Act
        _updateCallback.Invoke(default);

        // Assert
        Assert.That(vm.TotalMutationCount.Value, Is.EqualTo(2));
        Assert.That(vm.KilledMutationCount.Value, Is.Zero);
        Assert.That(vm.SurvivedMutationCount.Value, Is.Zero);

        // validCount = 0 → score should remain unchanged (default 0)
        Assert.That(vm.MutationScore.Value, Is.Zero);
    }

    private DiscoveredMutation CreateMutation(MutantStatus status)
    {
        DiscoveredMutation mutation = new(new SyntaxAnnotation(), SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), _eventAggregator)
        {
            Status = status
        };

        return mutation;
    }
}
