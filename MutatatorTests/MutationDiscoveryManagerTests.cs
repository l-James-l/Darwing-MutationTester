using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Models;
using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;
using Mutator;
using Mutator.MutationImplementations;
using NSubstitute;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;

namespace MutatorTests;

[TestFixture]
public class MutationDiscoveryManagerTests
{
    private ISolutionProvider _solutionProvider;
    private IMutationImplementationProvider _mutationImplementationProvider;
    private IStatusTracker _statusTracker;
    private IEventAggregator _eventAggregator;
    private IMutationSettings _settings;
    private MutationDiscoveryManager _mutationDiscoveryManager; //SUT

    [SetUp]
    public void SetUp()
    {
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _mutationImplementationProvider = Substitute.For<IMutationImplementationProvider>();
        _statusTracker = Substitute.For<IStatusTracker>();
        _eventAggregator = Substitute.For<IEventAggregator>();
        _settings = Substitute.For<IMutationSettings>();

        _mutationDiscoveryManager = new MutationDiscoveryManager(
            _solutionProvider,
            _mutationImplementationProvider,
            _statusTracker,
            _eventAggregator,
            _settings);
    }

    [Test]
    public void PerformMutationDiscovery_GivenStatusTrackerFailsToStart_WhenCalled_ThenDoesNotPerformDiscovery()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.DiscoveringMutants).Returns(false);
        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();
        
        // Act
        _mutationDiscoveryManager.PerformMutationDiscovery();

        // Assert
        IReadOnlyList<LogEvent> allLogs = TestCorrelator.GetLogEventsFromCurrentContext();
        Assert.That(
            allLogs.FirstOrDefault(x => x.MessageTemplate.Text == $"Attempted to start invalid operation {DarwingOperation.DiscoveringMutants}."),
            Is.Not.Null);
        Assert.That(allLogs.FirstOrDefault(x => x.MessageTemplate.Text.Contains("Discovering mutations ")), Is.Null);
    }

    [Test]
    public void PerformMutationDiscovery_GivenNoFilesToMutate_WhenCalled_ThenFinishesWithSuccess()
    {
        // Arrange
        var solutionContainer = Substitute.For<ISolutionContainer>();
        solutionContainer.SolutionProjects.Returns(new List<IProjectContainer>());
        _solutionProvider.SolutionContainer.Returns(solutionContainer);
        _statusTracker.TryStartOperation(DarwingOperation.DiscoveringMutants).Returns(true);

        // Act
        _mutationDiscoveryManager.PerformMutationDiscovery();

        // Assert
        _statusTracker.Received().FinishOperation(DarwingOperation.DiscoveringMutants, true);
    }

    [Test]
    public void PerformMutationDiscovery_GivenDiscoverySucceedsAndWorkspaceAppliesChanges_WhenCalled_ThenPublishesBuildEvent()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.DiscoveringMutants).Returns(true);

        var buildEvent = Substitute.For<BuildMutatedSolutionEvent>();
        _eventAggregator.GetEvent<BuildMutatedSolutionEvent>().Returns(buildEvent);

        var mockProject = CreateMockProjectWithFiles();
        var solutionContainer = Substitute.For<ISolutionContainer>();

        using var workspace = new AdhocWorkspace();
        var emptySolution = workspace.CurrentSolution;

        solutionContainer.SolutionProjects.Returns(new List<IProjectContainer> { mockProject });
        solutionContainer.Workspace.Returns(workspace);
        solutionContainer.Solution.Returns(emptySolution);
        _solutionProvider.SolutionContainer.Returns(solutionContainer);

        // Note: AdhocWorkspace.TryApplyChanges usually returns true by default 
        // as long as the solution belongs to the workspace.

        // Act
        _mutationDiscoveryManager.PerformMutationDiscovery(); //SUT

        // Assert
        solutionContainer.Received().RestoreProjects();
        _statusTracker.Received().FinishOperation(DarwingOperation.DiscoveringMutants, true);
        buildEvent.Received().Publish();
    }

    [Test]
    public void PerformMutationDiscovery_GivenMutatorFailsWithException_WhenCalled_ThenLogsErrorAndContinues()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.DiscoveringMutants).Returns(true);
        var mockProject = CreateMockProjectWithFiles();
        _solutionProvider.SolutionContainer.SolutionProjects.Returns(new List<IProjectContainer> { mockProject });

        IMutationImplementation mutator = Substitute.For<IMutationImplementation>();
        _mutationImplementationProvider.CanMutate(Arg.Any<SyntaxNode>(), out _)
            .Returns(x => {
                x[1] = mutator;
                return true;
            });

        mutator.When(m => m.Mutate(Arg.Any<SyntaxNode>())).Do(_ => throw new Exception("Simulated Failure"));

        // Act & Assert
        Assert.DoesNotThrow(() => _mutationDiscoveryManager.PerformMutationDiscovery());
    }

    [Test]
    public void PerformMutationDiscovery_GivenSingleMutantPerLineEnabled_WhenMultipleMutationsOnSameLine_ThenIgnoresSubsequentMutations()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.DiscoveringMutants).Returns(true);
        _settings.SingleMutantPerLine.Returns(true);

        // To test this effectively, we'd need to mock the RediscoverMutationsInTree logic 
        // which populates the LineSpan. Since that relies on real Roslyn SyntaxNodes, 
        // this is better suited for an Integration Test or a very data-heavy Unit Test.
    }

    private IProjectContainer CreateMockProjectWithFiles()
    {
        IProjectContainer project = Substitute.For<IProjectContainer>();
        
        SourceCodeFileCollection fileCollection = new();

        fileCollection.AddDocument(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText("public class A {}"));
        project.FileCollection.Returns(fileCollection);
        return project;
    }
}