using Core;
using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Enums;
using Models.Events;
using Models.SharedInterfaces;
using Mutator;
using NSubstitute;
using System.Diagnostics;

namespace CoreTests;

public class InitialTestRunnerTests
{
    private InitialTestRunner _runner; // SUT

    private IEventAggregator _eventAggregator;
    private IMutationSettings _mutationSettings;
    private IProcessWrapperFactory _processWrapperFactory;
    private IMutationDiscoveryManager _mutationDiscoveryManager;
    private IStatusTracker _statusTracker;


    private InitialTestRunCompleteEvent _initiateTestRunCompleteEvent;

    [SetUp]
    public void SetUp()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _mutationSettings = Substitute.For<IMutationSettings>();
        _processWrapperFactory = Substitute.For<IProcessWrapperFactory>();
        _mutationDiscoveryManager = Substitute.For<IMutationDiscoveryManager>();
        _statusTracker = Substitute.For<IStatusTracker>();

        _runner = new InitialTestRunner(_eventAggregator, _mutationSettings, _statusTracker, _processWrapperFactory, _mutationDiscoveryManager);

        _initiateTestRunCompleteEvent = Substitute.For<InitialTestRunCompleteEvent>();
        _eventAggregator.GetEvent<InitialTestRunCompleteEvent>().Returns(_initiateTestRunCompleteEvent);
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
    public void GivenCanRun_WhenRun_ThenTestRunStarted()
    {
        //Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestUnmutatedSolution).Returns(true);

        _mutationSettings.SolutionPath.Returns("this/is/the/path/to/solution.sln");
        IProcessWrapper testProcess = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments == "test solution.sln --no-build")).Returns(testProcess);
        testProcess.StartAndAwait(null).Returns(true);
        testProcess.Success.Returns(true);
        testProcess.Output.Returns([]);
        testProcess.Errors.Returns([]);
        testProcess.Duration.Returns(TimeSpan.FromSeconds(30));


        //Act
        _runner.Run();

        //Assert
        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "test solution.sln --no-build" &&
            x.WorkingDirectory == "this\\is\\the\\path\\to" &&
            x.RedirectStandardError && x.RedirectStandardOutput));

        testProcess.Received(1).StartAndAwait(null);
        _mutationDiscoveryManager.Received(1).PerformMutationDiscovery();
        _initiateTestRunCompleteEvent.Received(1).Publish(Arg.Is<InitialTestRunInfo>(x => x.WasSuccesful && x.InitialRunDuration.Seconds == 30));
        _statusTracker.Received(1).FinishOperation(DarwingOperation.TestUnmutatedSolution, true);
    }

    [Test]
    public void GivenSuccessfulBuild_WhenPublishEvent_ThenTestRunStarted_AndTestRunFails_ThenResultAvailable()
    {
        //Arrange
        _statusTracker.TryStartOperation(DarwingOperation.TestUnmutatedSolution).Returns(true);
        _mutationSettings.SolutionPath.Returns("this/is/the/path/to/solution.sln");
        IProcessWrapper testProcess = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments == "test solution.sln --no-build")).Returns(testProcess);
        testProcess.StartAndAwait(null).Returns(true);
        testProcess.Success.Returns(false);
        testProcess.Output.Returns([]);
        testProcess.Errors.Returns([]);

        //Act
        _runner.Run();

        //Assert
        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "test solution.sln --no-build" &&
            x.WorkingDirectory == "this\\is\\the\\path\\to" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));
        testProcess.Received(1).StartAndAwait(null);
        _initiateTestRunCompleteEvent.Received(1).Publish(Arg.Is<InitialTestRunInfo>(x => !x.WasSuccesful));
        _mutationDiscoveryManager.DidNotReceiveWithAnyArgs().PerformMutationDiscovery();
        _statusTracker.Received(1).FinishOperation(DarwingOperation.TestUnmutatedSolution, false);
    }
}
