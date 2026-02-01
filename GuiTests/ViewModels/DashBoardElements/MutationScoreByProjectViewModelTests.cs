using GUI.ViewModels.DashBoardElements;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models;
using Models.Enums;
using Models.Events;
using Mutator;
using NSubstitute;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;

namespace GuiTests.ViewModels.DashBoardElements;

public class MutationScoreByProjectViewModelTests
{
    private IEventAggregator _eventAggregator;
    private IMutationDiscoveryManager _mutationDiscoveryManager;
    private ISolutionProvider _solutionProvider;

    private MutationUpdated _mutationUpdatedEvent;
    private DarwingOperationStatesChangedEvent _solutionLoadedEvent;

    private MutationScoreByProjectViewModel _vm;

    private Action<SyntaxAnnotation> _mutationUpdatedHandler = default!; //Assign default to make compiler happy 
    private Action<DarwingOperation> _solutionLoadedHandler = default!; //Assign default to make compiler happy 

    [SetUp]
    public void SetUp()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _mutationDiscoveryManager = Substitute.For<IMutationDiscoveryManager>();
        _solutionProvider = Substitute.For<ISolutionProvider>();

        _mutationUpdatedEvent = Substitute.For<MutationUpdated>();
        _solutionLoadedEvent = Substitute.For<DarwingOperationStatesChangedEvent>();

        _eventAggregator.GetEvent<MutationUpdated>().Returns(_mutationUpdatedEvent);
        _eventAggregator.GetEvent<DarwingOperationStatesChangedEvent>().Returns(_solutionLoadedEvent);

        // Capture the subscription callbacks
        _mutationUpdatedEvent
            .When(x => x.Subscribe(Arg.Any<Action<SyntaxAnnotation>>(), Arg.Is(ThreadOption.UIThread)))
            .Do(ci => _mutationUpdatedHandler = ci.Arg<Action<SyntaxAnnotation>>());

        _solutionLoadedEvent
            .When(x => x.Subscribe(
                Arg.Any<Action<DarwingOperation>>(),
                Arg.Is(ThreadOption.UIThread),
                Arg.Any<bool>(),
                Arg.Is<Predicate<DarwingOperation>>(x => x.Invoke(DarwingOperation.LoadSolution) && !x.Invoke(DarwingOperation.BuildSolution))))
            .Do(ci => _solutionLoadedHandler = ci.Arg<Action<DarwingOperation>>());

        _vm = new MutationScoreByProjectViewModel(_eventAggregator, _mutationDiscoveryManager, _solutionProvider);
    }

    [Test]
    public void GivenConstructed_SubscriptionsMade()
    {
        Assert.That(_mutationUpdatedHandler, Is.Not.Null);
        Assert.That(_solutionLoadedHandler, Is.Not.Null);
    }

    [Test]
    public void GivenSolutionAvailable_WhenSolutionLoaded_ThenProjectsAreCreated()
    {
        // Arrange
        IProjectContainer project1 = Substitute.For<IProjectContainer>();
        IProjectContainer project2 = Substitute.For<IProjectContainer>();

        ISolutionContainer solutionContainer = Substitute.For<ISolutionContainer>();
        solutionContainer.SolutionProjects.Returns([project1, project2]);

        _solutionProvider.IsAvailable.Returns(true);
        _solutionProvider.SolutionContainer.Returns(solutionContainer);

        // Act
        _solutionLoadedHandler.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.That(_vm.Projects.Count, Is.EqualTo(2));
    }

    [Test]
    public void GivenSolutionUnavailable_WhenSolutionLoaded_ThenProjectsAreClearedAndNotRepopulated()
    {
        // Arrange
        _vm.Projects.Add(new IndividualProjectSummaryViewModel(Substitute.For<IProjectContainer>()));
        _solutionProvider.IsAvailable.Returns(false);

        // Act
        _solutionLoadedHandler.Invoke(DarwingOperation.LoadSolution);

        // Assert
        Assert.That(_vm.Projects, Is.Empty);
    }

    [Test]
    public void GivenMutationNotDiscovered_WhenMutationUpdated_ThenNoProjectIsUpdated()
    {
        // Arrange
        SyntaxAnnotation id = new ();

        _mutationDiscoveryManager.DiscoveredMutations.Returns([]);

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        // Act
        _mutationUpdatedHandler.Invoke(id);

        // Assert
        LogEvent? log = TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x =>
            x.MessageTemplate.Text == "Received update for a mutation ID not present in the list of discovered mutations" &&
            x.Level == LogEventLevel.Error);
        Assert.That(log, Is.Not.Null);
    }

    [Test]
    public void GivenMutationsForProject_WhenMutationUpdated_ThenProjectCountsAreUpdated()
    {
        // Arrange
        ProjectId projectId = ProjectId.CreateNewId();

        IProjectContainer projectContainer = Substitute.For<IProjectContainer>();
        projectContainer.ID.Returns(projectId);

        IndividualProjectSummaryViewModel projectVm = new(projectContainer);
        _vm.Projects.Add(projectVm);

        SyntaxAnnotation id = new();

        DocumentId doc = DocumentId.CreateFromSerialized(projectId, Guid.NewGuid());
        doc.ProjectId.Returns(projectId);

        DiscoveredMutation mutation1 = new (id, SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), _eventAggregator)
        {
            Document = doc,
            Status = MutantStatus.Killed,
        };

        DiscoveredMutation mutation2 = new (new SyntaxAnnotation(), SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), _eventAggregator)
        {
            Document = doc,
            Status = MutantStatus.Survived
        };

        DiscoveredMutation mutation3 = new (new SyntaxAnnotation(), SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), _eventAggregator)
        {
            Document = doc,
            Status = MutantStatus.CausedBuildError
        };

        _mutationDiscoveryManager.DiscoveredMutations.Returns([mutation1, mutation2, mutation3]);

        // Act
        _mutationUpdatedHandler.Invoke(id);

        // Assert
        Assert.That(projectVm.TotalMutations, Is.EqualTo(2));      // excludes build error
        Assert.That(projectVm.KilledMutations, Is.EqualTo(1));
        Assert.That(projectVm.SurvivedMutations, Is.EqualTo(1));
    }

    [Test]
    public void GivenPropertyNotAssignedException_WhenMutationUpdated_ThenExceptionIsSwallowed()
    {
        // Arrange
        SyntaxAnnotation id = new ();
        DiscoveredMutation mutation = new(id, SyntaxFactory.EmptyStatement(), SyntaxFactory.EmptyStatement(), _eventAggregator);
        
        _mutationDiscoveryManager.DiscoveredMutations.Returns([mutation]);

        // Act / Assert
        Assert.DoesNotThrow(() => _mutationUpdatedHandler.Invoke(id));
    }

    [Test]
    public void Given_UnexpectedException_When_MutationUpdated_Then_ExceptionIsSwallowed()
    {
        // Arrange
        SyntaxAnnotation id = new();

        _mutationDiscoveryManager
            .DiscoveredMutations
            .Returns(x => throw new InvalidOperationException("boom"));

        // Act / Assert
        Assert.DoesNotThrow(() => _mutationUpdatedHandler.Invoke(id));
    }
}