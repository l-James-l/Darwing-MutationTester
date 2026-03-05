using Core;
using Core.IndustrialEstate;
using Core.Interfaces;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using Models;
using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;
using Mutator;
using NSubstitute;
using NSubstitute.ReturnsExtensions;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.TestCorrelator;
using System.Diagnostics;

namespace CoreTests;

public class MutatedSolutionTesterTests
{
    private IMutationDiscoveryManager _discoveryManager;
    private IProcessWrapperFactory _processFactory;
    private IMutationSettings _settings;
    private IStatusTracker _statusTracker;
    private ISolutionProvider _solutionProvider;
    private MutatedSolutionTester _mutantTester;

    [SetUp]
    public void SetUp()
    {
        _discoveryManager = Substitute.For<IMutationDiscoveryManager>();
        _processFactory = Substitute.For<IProcessWrapperFactory>();
        _settings = Substitute.For<IMutationSettings>();
        _statusTracker = Substitute.For<IStatusTracker>();
        _solutionProvider = Substitute.For<ISolutionProvider>();

        _settings.SolutionPath.Returns(@"C:\Repo\MySolution.sln");
        _settings.MutationTestTimeoutScaler.Returns(1.5);

        _mutantTester = new MutatedSolutionTester(
            _discoveryManager,
            _processFactory,
            _settings,
            _statusTracker,
            _solutionProvider);
    }

    [Test]
    public void GivenNoTestsRunSuccessfullyInitially_WhenRunTestsIsCalled_ThenAbortsAndReturnsFalse()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestMutants).Returns(true);
        _settings.SkipTestingNoActiveMutants.Returns(false);

        IProcessWrapper failProcess = Substitute.For<IProcessWrapper>();
        failProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        failProcess.Success.Returns(false); // Preliminary run failed
        failProcess.Output.Returns([]);
        failProcess.Errors.Returns([]);
        _processFactory.Create(Arg.Any<ProcessStartInfo>()).Returns(failProcess);

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        // Act
        _mutantTester.RunTestsOnMutatedSolution();

        // Assert
        _statusTracker.Received().FinishOperation(DarwingOperation.TestMutants, false);
        LogEvent? log = TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x => x.MessageTemplate.Text == "Introducing mutations caused tests to fail, cannot proceed with mutation testing.");
        Assert.That(log, Is.Not.Null);
        log = TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x => x.MessageTemplate.Text == "Testing mutant {mutant} in {file}.");
        Assert.That(log, Is.Null);

    }

    [Test]
    public void GivenMutantIsKilledByFailingTest_WhenRunTestsIsCalled_ThenMutantStatusIsKilled()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestMutants).Returns(true);
        _settings.SkipTestingNoActiveMutants.Returns(true);

        // Setup a mutant
        DiscoveredMutation mutant = CreateMockMutant();
        _discoveryManager.DiscoveredMutations.Returns(new List<DiscoveredMutation> { mutant });

        // Setup File Mapping for coverage
        IProjectContainer testProject = Substitute.For<IProjectContainer>();
        SourceCodeFileCollection fileCollection = new ();
        fileCollection.AddDocument(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText(""));
        testProject.DirectoryPath.Returns(@"C:\Repo\Tests");
        testProject.FileCollection.Returns(fileCollection);

        TestInfo testInfo = new TestInfo(testProject, "MyTest", TimeSpan.FromSeconds(1));

        SourceCodeFileContainer fileContainer = testProject.FileCollection.First();
        fileContainer.LineToTestMapping.Add(1, [testInfo]);

        _solutionProvider.SolutionContainer.FindFile(Arg.Any<DocumentId>()).Returns(fileContainer);

        // Mock a FAILED test process (Mutant Killed)
        var killedProcess = Substitute.For<IProcessWrapper>();
        killedProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        killedProcess.Success.Returns(false);
        killedProcess.Output.Returns([]);
        killedProcess.Errors.Returns([]);
        _processFactory.Create(Arg.Any<ProcessStartInfo>()).Returns(killedProcess);

        // Act
        _mutantTester.RunTestsOnMutatedSolution();

        // Assert
        Assert.That(mutant.Status, Is.EqualTo(MutantStatus.Killed));
        _statusTracker.Received().FinishOperation(DarwingOperation.TestMutants, true);
    }

    [Test]
    public void GivenMutantCausesInfiniteLoop_WhenRunTestsIsCalled_ThenMutantStatusIsKilledByTimeout()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestMutants).Returns(true);
        _settings.SkipTestingNoActiveMutants.Returns(true);

        // Setup a mutant
        DiscoveredMutation mutant = CreateMockMutant();
        _discoveryManager.DiscoveredMutations.Returns(new List<DiscoveredMutation> { mutant });

        // Setup File Mapping for coverage
        IProjectContainer testProject = Substitute.For<IProjectContainer>();
        SourceCodeFileCollection fileCollection = new();
        fileCollection.AddDocument(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText(""));
        testProject.DirectoryPath.Returns(@"C:\Repo\Tests");
        testProject.FileCollection.Returns(fileCollection);

        TestInfo testInfo = new TestInfo(testProject, "MyTest", TimeSpan.FromSeconds(1));

        SourceCodeFileContainer fileContainer = testProject.FileCollection.First();
        fileContainer.LineToTestMapping.Add(1, [testInfo]);

        _solutionProvider.SolutionContainer.FindFile(Arg.Any<DocumentId>()).Returns(fileContainer);

        // Mock a FAILED test process (Mutant Killed)
        var killedProcess = Substitute.For<IProcessWrapper>();
        killedProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(false);
        killedProcess.Success.Returns(true);
        killedProcess.Output.Returns([]);
        killedProcess.Errors.Returns([]);
        _processFactory.Create(Arg.Any<ProcessStartInfo>()).Returns(killedProcess);

        // Act
        _mutantTester.RunTestsOnMutatedSolution();

        // Assert
        Assert.That(mutant.Status, Is.EqualTo(MutantStatus.KilledByTimeOut));
    }

    [Test]
    public void GivenMutantHasNoCoverage_WhenRunTestsIsCalled_ThenStatusIsNoCoverageAndNoProcessStarts()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestMutants).Returns(true);
        _settings.SkipTestingNoActiveMutants.Returns(true);

        // Setup a mutant
        DiscoveredMutation mutant = CreateMockMutant();
        _discoveryManager.DiscoveredMutations.Returns(new List<DiscoveredMutation> { mutant });

        // Setup File Mapping for coverage
        IProjectContainer testProject = Substitute.For<IProjectContainer>();
        SourceCodeFileCollection fileCollection = new();
        fileCollection.AddDocument(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText(""));
        testProject.DirectoryPath.Returns(@"C:\Repo\Tests");
        testProject.FileCollection.Returns(fileCollection);

        SourceCodeFileContainer fileContainer = testProject.FileCollection.First();
        fileContainer.LineToTestMapping.Add(1, []); //No tests

        _solutionProvider.SolutionContainer.FindFile(Arg.Any<DocumentId>()).Returns(fileContainer);

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        // Act
        _mutantTester.RunTestsOnMutatedSolution();

        // Assert
        Assert.That(mutant.Status, Is.EqualTo(MutantStatus.NoCoverage));
        _processFactory.DidNotReceive().Create(Arg.Is<ProcessStartInfo>(p => p.Arguments.Contains("--filter")));
        LogEvent? log = TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x => x.MessageTemplate.Text == "No coverage for mutant.");
        Assert.That(log, Is.Not.Null);
    }

    [Test]
    public void GivenMutantFileNotFoundThusHasNoCoverage_WhenRunTestsIsCalled_ThenStatusIsNoCoverageAndNoProcessStarts()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestMutants).Returns(true);
        _settings.SkipTestingNoActiveMutants.Returns(true);

        // Setup a mutant
        DiscoveredMutation mutant = CreateMockMutant();
        _discoveryManager.DiscoveredMutations.Returns(new List<DiscoveredMutation> { mutant });

        // Setup File Mapping for coverage
        IProjectContainer testProject = Substitute.For<IProjectContainer>();
        SourceCodeFileCollection fileCollection = new();
        testProject.DirectoryPath.Returns(@"C:\Repo\Tests");
        testProject.FileCollection.Returns(fileCollection);

        _solutionProvider.SolutionContainer.FindFile(Arg.Any<DocumentId>()).ReturnsNull();

        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        // Act
        _mutantTester.RunTestsOnMutatedSolution();

        // Assert
        Assert.That(mutant.Status, Is.EqualTo(MutantStatus.NoCoverage));
        _processFactory.DidNotReceive().Create(Arg.Is<ProcessStartInfo>(p => p.Arguments.Contains("--filter")));
        LogEvent? log = TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x => x.MessageTemplate.Text == "Mutant file not found. Setting no coverage.");
        Assert.That(log, Is.Not.Null);

    }

    [Test]
    public void GivenMutantIsNotKilledByFailingTest_WhenRunTestsIsCalled_ThenMutantStatusIsSurvived()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestMutants).Returns(true);
        _settings.SkipTestingNoActiveMutants.Returns(true);

        // Setup a mutant
        DiscoveredMutation mutant = CreateMockMutant();
        _discoveryManager.DiscoveredMutations.Returns(new List<DiscoveredMutation> { mutant });

        // Setup File Mapping for coverage
        IProjectContainer testProject = Substitute.For<IProjectContainer>();
        SourceCodeFileCollection fileCollection = new();
        fileCollection.AddDocument(DocumentId.CreateNewId(ProjectId.CreateNewId()), CSharpSyntaxTree.ParseText(""));
        testProject.DirectoryPath.Returns(@"C:\Repo\Tests");
        testProject.FileCollection.Returns(fileCollection);

        TestInfo testInfo = new TestInfo(testProject, "MyTest", TimeSpan.FromSeconds(1));

        SourceCodeFileContainer fileContainer = testProject.FileCollection.First();
        fileContainer.LineToTestMapping.Add(1, [testInfo]);

        _solutionProvider.SolutionContainer.FindFile(Arg.Any<DocumentId>()).Returns(fileContainer);

        // Mock a FAILED test process (Mutant Killed)
        var killedProcess = Substitute.For<IProcessWrapper>();
        killedProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        killedProcess.Success.Returns(true);
        killedProcess.Output.Returns([]);
        killedProcess.Errors.Returns([]);
        _processFactory.Create(Arg.Any<ProcessStartInfo>()).Returns(killedProcess);

        // Act
        _mutantTester.RunTestsOnMutatedSolution();

        // Assert
        Assert.That(mutant.Status, Is.EqualTo(MutantStatus.Survived));
        _statusTracker.Received().FinishOperation(DarwingOperation.TestMutants, true);
    }

    private DiscoveredMutation CreateMockMutant()
    {
        IEventAggregator eventAggregator = Substitute.For<IEventAggregator>();
        eventAggregator.GetEvent<MutationUpdated>().Returns(new MutationUpdated());

        return new DiscoveredMutation(new SyntaxAnnotation(), CSharpSyntaxTree.ParseText("a == b").GetRoot(),
             SyntaxFactory.EmptyStatement(), CSharpSyntaxTree.ParseText("a != b").GetRoot(), eventAggregator, 0, 0)
        {
            Document = DocumentId.CreateNewId(ProjectId.CreateNewId()),
            LineSpan = new FileLinePositionSpan("test.cs", new LinePosition(0, 0), new LinePosition(0, 10)),
            Status = MutantStatus.Available
        };
    }
}