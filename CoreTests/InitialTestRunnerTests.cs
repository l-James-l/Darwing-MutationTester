using NSubstitute;
using Core;
using Core.Interfaces;
using Models;
using Models.Enums;
using System.Diagnostics;
using Models.SharedInterfaces;
using Core.IndustrialEstate;
using Mutator;
using Serilog;
using Serilog.Sinks.TestCorrelator;

namespace CoreTests;

public class InitialTestRunnerTests
{
    private IMutationSettings _mutationSettings;
    private IStatusTracker _statusTracker;
    private IProcessWrapperFactory _processWrapperFactory;
    private IMutationDiscoveryManager _mutationDiscoveryManager;
    private ISolutionProvider _solutionProvider;
    private ICoverageMapper _coverageMapper;

    private InitialTestRunner _runner;

    [SetUp]
    public void SetUp()
    {
        _mutationSettings = Substitute.For<IMutationSettings>();
        _statusTracker = Substitute.For<IStatusTracker>();
        _processWrapperFactory = Substitute.For<IProcessWrapperFactory>();
        _mutationDiscoveryManager = Substitute.For<IMutationDiscoveryManager>();
        _solutionProvider = Substitute.For<ISolutionProvider>();
        _coverageMapper = Substitute.For<ICoverageMapper>();

        _mutationSettings.TestRunTimeout.Returns(30);

        _runner = new InitialTestRunner(
            _mutationSettings,
            _statusTracker,
            _processWrapperFactory,
            _mutationDiscoveryManager,
            _solutionProvider,
            _coverageMapper);
    }

    [Test]
    public void GivenOperationAlreadyRunning_WhenRunIsCalled_ThenDoesNotProceed()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestUnmutatedSolution).Returns(false);

        // Act
        _runner.Run();

        // Assert
        _processWrapperFactory.DidNotReceiveWithAnyArgs().Create(Arg.Any<ProcessStartInfo>());
    }

    [Test]
    public void GivenAltCoverInstallFails_WhenRunIsCalled_ThenFinishesOperationWithFailure()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestUnmutatedSolution).Returns(true);

        IProcessWrapper installProcess = Substitute.For<IProcessWrapper>();
        installProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        installProcess.Success.Returns(false); // Install failed
        installProcess.Output.Returns([]);
        installProcess.Errors.Returns([]);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(i => i.Arguments.Contains("tool install")))
                       .Returns(installProcess);
        Log.Logger = new LoggerConfiguration().WriteTo.TestCorrelator().CreateLogger();
        TestCorrelator.CreateContext();

        // Act
        _runner.Run();

        // Assert
        _statusTracker.Received().FinishOperation(DarwingOperation.TestUnmutatedSolution, false);
        _mutationDiscoveryManager.DidNotReceive().PerformMutationDiscovery();
        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" && x.Arguments == "tool install -g altcover.global"));
        Assert.That(TestCorrelator.GetLogEventsFromCurrentContext().FirstOrDefault(x => x.MessageTemplate.Text == "Unable to install altcover. Testing cannot be done using coverage."), Is.Not.Null);
    }

    [Test]
    public void GivenSuccessfulTestRun_WhenRunIsCalled_ThenTriggersMutationDiscovery()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestUnmutatedSolution).Returns(true);

        // Mock AltCover Install
        IProcessWrapper installProcess = Substitute.For<IProcessWrapper>();
        installProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        installProcess.Success.Returns(true);
        installProcess.Output.Returns([]);
        installProcess.Errors.Returns([]);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(i => i.Arguments.Contains("tool install")))
                       .Returns(installProcess);

        // Mock Test Project and Process
        IProjectContainer testProject = Substitute.For<IProjectContainer>();
        testProject.Name.Returns("TestProj");
        testProject.OutputDirectory.Returns(Path.GetTempPath());
        testProject.CsprojFilePath.Returns("test.csproj");

        ISolutionContainer solutionContainer = Substitute.For<ISolutionContainer>();
        solutionContainer.TestProjects.Returns([testProject]);
        _solutionProvider.SolutionContainer.Returns(solutionContainer);

        IProcessWrapper testProcess = Substitute.For<IProcessWrapper>();
        testProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        testProcess.Success.Returns(true);
        testProcess.Output.Returns([]);
        testProcess.Errors.Returns([]);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(i => i.FileName == "altcover" && !i.Arguments.Contains("--collect")))
                       .Returns(testProcess);

        // Mock Collection and Mapping
        var collectProcess = Substitute.For<IProcessWrapper>();
        collectProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        collectProcess.Success.Returns(true);
        collectProcess.Output.Returns([]);
        collectProcess.Errors.Returns([]);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(i => i.Arguments.Contains("--collect")))
                       .Returns(collectProcess);

        _coverageMapper.MapFullCoverage(Arg.Any<string>()).Returns(true);

        // Act
        _runner.Run();

        // Assert
        installProcess.Received(1).StartAndAwait(Arg.Is<TimeSpan>(x => x.CompareTo(TimeSpan.FromSeconds(60)) == 0));
        testProcess.Received(1).StartAndAwait(Arg.Is<TimeSpan>(x => x.CompareTo(TimeSpan.FromSeconds(_mutationSettings.TestRunTimeout)) == 0));
        collectProcess.Received(1).StartAndAwait(Arg.Is<TimeSpan>(x => x.CompareTo(TimeSpan.FromSeconds(60)) == 0));
        _coverageMapper.Received(1).MapFullCoverage(Path.Combine(testProject.OutputDirectory, "DarwingCoverage.xml"));
        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "altcover" &&
            !x.Arguments.StartsWith("runner") &&
            x.Arguments.Contains("--inplace") &&
            x.Arguments.Contains("--save") &&
            x.Arguments.Contains("--linecover") &&
            x.Arguments.Contains("--all") &&
            x.Arguments.Contains("-c \"[Test]\"") &&
            x.Arguments.Contains("-c \"[Fact]\"") &&
            x.Arguments.Contains("-c \"[Theory]\"") &&
            x.Arguments.Contains("-c \"[TestMethod]\"") &&
            x.Arguments.Contains($"--inputDirectory \"{testProject.OutputDirectory}\"") &&
            x.Arguments.Contains("--outputDirectory \"DarwingOriginalSavedBinaries\"") &&
            x.Arguments.Contains("--report \"DarwingCoverage.xml\"") &&
            x.Arguments.EndsWith($"-- dotnet test \"{testProject.CsprojFilePath}\" --no-build --no-restore -- --stop-on-failure")
            ));
        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "altcover" &&
            x.Arguments == $"runner --collect --recorderDirectory \"{testProject.OutputDirectory}\""));
        _statusTracker.Received().FinishOperation(DarwingOperation.TestUnmutatedSolution, true);
        _mutationDiscoveryManager.Received(1).PerformMutationDiscovery();
    }

    [Test]
    public void GivenExceptionInRunner_WhenRunIsCalled_ThenStatusIsMarkedAsFailed()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestUnmutatedSolution).Returns(true);
        _processWrapperFactory.When(x => x.Create(Arg.Any<ProcessStartInfo>())).Do(x => { throw new Exception("Boom"); });

        // Act
        _runner.Run();

        // Assert
        _statusTracker.Received().FinishOperation(DarwingOperation.TestUnmutatedSolution, false);
    }

    [Test]
    public void GivenNoBackupDirectory_WhenRestoreIsCalled_ThenReturnsFalse()
    {
        // Arrange
        IProjectContainer testProject = Substitute.For<IProjectContainer>();
        testProject.OutputDirectory.Returns(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

        // Act
        bool result = _runner.Restore(testProject);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void GivenStatusTrackerSaysNo_WhenRun_ThenNoProcessStarted()
    {
        //Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestUnmutatedSolution).Returns(false);

        //Act
        _runner.Run();

        //Assert
        _processWrapperFactory.Received(0).Create(Arg.Any<ProcessStartInfo>());
    }


    [Test]
    public void WhenRun_AndCollectCoverageProcessFailed_ThenMutationDiscoveryNotStarted()
    {
        // Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestUnmutatedSolution).Returns(true);

        // Mock AltCover Install
        IProcessWrapper installProcess = Substitute.For<IProcessWrapper>();
        installProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        installProcess.Success.Returns(true);
        installProcess.Output.Returns([]);
        installProcess.Errors.Returns([]);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(i => i.Arguments.Contains("tool install")))
                       .Returns(installProcess);

        // Mock Test Project and Process
        IProjectContainer testProject = Substitute.For<IProjectContainer>();
        testProject.Name.Returns("TestProj");
        testProject.OutputDirectory.Returns(Path.GetTempPath());
        testProject.CsprojFilePath.Returns("test.csproj");

        ISolutionContainer solutionContainer = Substitute.For<ISolutionContainer>();
        solutionContainer.TestProjects.Returns([testProject]);
        _solutionProvider.SolutionContainer.Returns(solutionContainer);

        IProcessWrapper testProcess = Substitute.For<IProcessWrapper>();
        testProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        testProcess.Success.Returns(true);
        testProcess.Output.Returns([]);
        testProcess.Errors.Returns([]);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(i => i.FileName == "altcover" && !i.Arguments.Contains("--collect")))
                       .Returns(testProcess);

        // Mock Collection and Mapping
        var collectProcess = Substitute.For<IProcessWrapper>();
        collectProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        collectProcess.Success.Returns(false);
        collectProcess.Output.Returns([]);
        collectProcess.Errors.Returns([]);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(i => i.Arguments.Contains("--collect")))
                       .Returns(collectProcess);

        _coverageMapper.MapFullCoverage(Arg.Any<string>()).Returns(true);

        // Act
        _runner.Run();

        // Assert
        installProcess.Received(1).StartAndAwait(Arg.Is<TimeSpan>(x => x.CompareTo(TimeSpan.FromSeconds(60)) == 0));
        testProcess.Received(1).StartAndAwait(Arg.Is<TimeSpan>(x => x.CompareTo(TimeSpan.FromSeconds(_mutationSettings.TestRunTimeout)) == 0));
        collectProcess.Received(1).StartAndAwait(Arg.Is<TimeSpan>(x => x.CompareTo(TimeSpan.FromSeconds(60)) == 0));
        _coverageMapper.DidNotReceive().MapFullCoverage(Arg.Any<string>());

        _statusTracker.Received().FinishOperation(DarwingOperation.TestUnmutatedSolution, false);
        _mutationDiscoveryManager.DidNotReceive().PerformMutationDiscovery();
    }

    [Test]
    public void WhenStart_AndTestRunFails_ThenProcessMarkedAsFailed_AndMutationDiscoveryNotStarted()
    {
        //Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestUnmutatedSolution).Returns(true);

        // Mock AltCover Install
        IProcessWrapper installProcess = Substitute.For<IProcessWrapper>();
        installProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        installProcess.Success.Returns(true);
        installProcess.Output.Returns([]);
        installProcess.Errors.Returns([]);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(i => i.Arguments.Contains("tool install")))
                       .Returns(installProcess);

        // Mock Test Project and Process
        IProjectContainer testProject = Substitute.For<IProjectContainer>();
        testProject.Name.Returns("TestProj");
        testProject.OutputDirectory.Returns(Path.GetTempPath());
        testProject.CsprojFilePath.Returns("test.csproj");

        ISolutionContainer solutionContainer = Substitute.For<ISolutionContainer>();
        solutionContainer.TestProjects.Returns([testProject]);
        _solutionProvider.SolutionContainer.Returns(solutionContainer);

        IProcessWrapper testProcess = Substitute.For<IProcessWrapper>();
        testProcess.StartAndAwait(Arg.Any<TimeSpan>()).Returns(true);
        testProcess.Success.Returns(false);
        testProcess.Output.Returns([]);
        testProcess.Errors.Returns([]);
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(i => i.FileName == "altcover" && !i.Arguments.Contains("--collect")))
                       .Returns(testProcess);

        //Act
        _runner.Run();

        //Assert
        testProcess.Received(1).StartAndAwait(Arg.Is<TimeSpan>(x => x.CompareTo(TimeSpan.FromSeconds(_mutationSettings.TestRunTimeout)) == 0));
        _mutationDiscoveryManager.DidNotReceiveWithAnyArgs().PerformMutationDiscovery();
        _statusTracker.Received(1).FinishOperation(DarwingOperation.TestUnmutatedSolution, false);
    }
}
