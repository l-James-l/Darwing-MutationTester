using Core;
using Core.IndustrialEstate;
using Core.Interfaces;
using Models;
using Models.Events;
using Mutator;
using NSubstitute;
using System.Diagnostics;

namespace CoreTests;

public class InitialTestRunnerTests
{
    private InitialTestRunnner _runner; // SUT

    private IEventAggregator _eventAggregator;
    private IMutationSettings _mutationSettings;
    private IWasBuildSuccessfull _buildSuccessfull;
    private IProcessWrapperFactory _processWrapperFactory;
    private IMutationRunManager _mutationRunManager;

    private InitiateTestRunEvent _initiateTestRunEvent;

    [SetUp]
    public void SetUp()
    {
        _eventAggregator = Substitute.For<IEventAggregator>();
        _mutationSettings = Substitute.For<IMutationSettings>();
        _buildSuccessfull = Substitute.For<IWasBuildSuccessfull>();
        _processWrapperFactory = Substitute.For<IProcessWrapperFactory>();
        _mutationRunManager = Substitute.For<IMutationRunManager>();

        _runner = new InitialTestRunnner(_eventAggregator, _mutationSettings, _buildSuccessfull, _processWrapperFactory, _mutationRunManager);

        _initiateTestRunEvent = new InitiateTestRunEvent();
        _eventAggregator.GetEvent<InitiateTestRunEvent>().Returns(_initiateTestRunEvent);
    }

    [Test]
    public void WhenStartUp_ThenTestrunEventSubscribed()
    {
        //Arrange
        InitiateTestRunEvent testRunEvent = Substitute.For<InitiateTestRunEvent>();
        _eventAggregator.GetEvent<InitiateTestRunEvent>().Returns(testRunEvent);

        //Act
        _runner.StartUp();

        //Assert
        testRunEvent.Received(1).Subscribe(Arg.Any<Action>());
    }

    [Test]
    public void GivenLastBuildNotSuccessful_WhenPublishEvent_ThenNoProcessStarted()
    {
        //Arrange
        _buildSuccessfull.WasLastBuildSuccessful.Returns(false);
        _runner.StartUp();

        //Act
        _initiateTestRunEvent.Publish();

        //Assert
        _processWrapperFactory.Received(0).Create(Arg.Any<ProcessStartInfo>());
    }

    [Test]
    public void GivenSuccessfulBuild_WhenPublishEvent_ThenTestRunStarted()
    {
        //Arrange
        _buildSuccessfull.WasLastBuildSuccessful.Returns(true);
        _mutationSettings.SolutionPath.Returns("this/is/the/path/to/solution.sln");
        IProcessWrapper testProcess = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments == "test solution.sln --no-build")).Returns(testProcess);
        testProcess.StartAndAwait(null).Returns(true);
        testProcess.Success.Returns(true);
        testProcess.Output.Returns([]);
        testProcess.Errors.Returns([]);

        _runner.StartUp();

        //Act
        _initiateTestRunEvent.Publish();

        //Assert
        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "test solution.sln --no-build" &&
            x.WorkingDirectory == "this\\is\\the\\path\\to" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));
        testProcess.Received(1).StartAndAwait(null);
        _mutationRunManager.Received(1).Run(Arg.Is<InitialTestRunInfo>(x => x.WasSuccesful));
    }

    [Test]
    public void GivenSuccessfulBuild_WhenPublishEvent_ThenTestRunStarted_AndTestRunFails_ThenResultAvailable()
    {
        //Arrange
        _buildSuccessfull.WasLastBuildSuccessful.Returns(true);
        _mutationSettings.SolutionPath.Returns("this/is/the/path/to/solution.sln");
        IProcessWrapper testProcess = Substitute.For<IProcessWrapper>();
        _processWrapperFactory.Create(Arg.Is<ProcessStartInfo>(x => x.Arguments == "test solution.sln --no-build")).Returns(testProcess);
        testProcess.StartAndAwait(null).Returns(true);
        testProcess.Success.Returns(false);
        testProcess.Output.Returns([]);
        testProcess.Errors.Returns([]);

        _runner.StartUp();

        //Act
        _initiateTestRunEvent.Publish();

        //Assert
        _processWrapperFactory.Received(1).Create(Arg.Is<ProcessStartInfo>(x =>
            x.FileName == "dotnet" &&
            x.Arguments == "test solution.sln --no-build" &&
            x.WorkingDirectory == "this\\is\\the\\path\\to" &&
            x.RedirectStandardError && x.RedirectStandardOutput && !x.UseShellExecute));
        testProcess.Received(1).StartAndAwait(null);
        _mutationRunManager.DidNotReceiveWithAnyArgs().Run(default!);
    }
}
